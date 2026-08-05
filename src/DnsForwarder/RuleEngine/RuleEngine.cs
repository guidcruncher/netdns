using System.Net;
using System.Text.RegularExpressions;

using DnsForwarder.Filtering;

using Microsoft.Extensions.Logging;

namespace DnsForwarder.RuleEngine;

public sealed class RuleEngine
{
    private readonly ILogger<RuleEngine> _logger;
    private readonly DnsForwarderOptions _options;
    private readonly IDnsClient _defaultClient;

    // Host matching (exact + wildcard, most-specific wins)
    private readonly HostMatcher _hosts = new();

    // Rule matching
    private readonly Dictionary<string, CompiledRule> _exact = new(StringComparer.OrdinalIgnoreCase);
    private readonly SuffixTrie _suffix = new();
    private readonly PrefixTrie _prefix = new();
    private readonly AhoCorasickMatcher _aho = new();
    private readonly List<CompiledRule> _regex = new();
    private readonly List<UpstreamEntry> _fallback = new();

    public DnsCache Cache { get; } = new();

    public RuleEngine(DnsForwarderOptions options, ILogger<RuleEngine> logger)
    {
        _options = options;
        _logger = logger;

        _defaultClient = new UdpDnsClient(
            new IPEndPoint(IPAddress.Parse(options.DefaultResolver.Address),
                           options.DefaultResolver.Port));

        if (options.Resolvers != null)
        {
            foreach (var r in options.Resolvers)
                AddResolver(r);
        }

        _aho.Build();
    }

    // ---------------------------------------------------------------------
    // BLOCK RESPONSE HANDLING
    // ---------------------------------------------------------------------

    private byte[] BuildBlockResponse(byte[] request)
    {
        var mode = _options.BlockResponse.Mode.ToUpperInvariant();

        return mode switch
        {
            "NXDOMAIN" => BuildRcodeResponse(request, rcode: 3),
            "SERVFAIL" => BuildRcodeResponse(request, rcode: 2),
            "REFUSED" => BuildRcodeResponse(request, rcode: 5),
            "STATIC_IP" => BuildStaticIpResponse(request, IPAddress.Parse(_options.BlockResponse.StaticIp)),
            _ => BuildRcodeResponse(request, rcode: 3)
        };
    }

    private static byte[] BuildRcodeResponse(byte[] req, int rcode)
    {
        var resp = new List<byte>();

        // Transaction ID
        resp.Add(req[0]);
        resp.Add(req[1]);

        // Flags: QR=1, RD=1, RA=1, RCODE=rcode
        resp.Add(0x81);
        resp.Add((byte)(0x80 | (rcode & 0x0F)));

        // QDCOUNT = 1
        resp.Add(0x00);
        resp.Add(0x01);

        // ANCOUNT = 0
        resp.Add(0x00);
        resp.Add(0x00);

        // NSCOUNT = 0, ARCOUNT = 0
        resp.Add(0x00);
        resp.Add(0x00);
        resp.Add(0x00);
        resp.Add(0x00);

        // Copy question section
        resp.AddRange(req.Skip(12));

        return resp.ToArray();
    }

    private byte[] BuildStaticIpResponse(byte[] req, IPAddress ip)
    {
        ushort id = (ushort)((req[0] << 8) | req[1]);

        var response = new List<byte>
        {
            (byte)(id >> 8), (byte)(id & 0xFF),
            0x81, 0x80, // QR=1, RD=1, RA=1, RCODE=0
            0x00, 0x01, // QDCOUNT
            0x00, 0x01, // ANCOUNT
            0x00, 0x00, // NSCOUNT
            0x00, 0x00  // ARCOUNT
        };

        // Copy question
        response.AddRange(req.Skip(12));

        // Answer section
        response.Add(0xC0);
        response.Add(0x0C);

        var addrBytes = ip.GetAddressBytes();

        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            response.AddRange(new byte[] { 0x00, 0x01 }); // A
        else
            response.AddRange(new byte[] { 0x00, 0x1C }); // AAAA

        response.AddRange(new byte[] { 0x00, 0x01 }); // CLASS IN

        // TTL
        response.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(_options.BlockResponse.Ttl)));

        // RDLENGTH + RDATA
        response.Add(0x00);
        response.Add((byte)addrBytes.Length);
        response.AddRange(addrBytes);

        return response.ToArray();
    }

    // ---------------------------------------------------------------------
    // RESOLVER RULE LOADING
    // ---------------------------------------------------------------------

    private void AddResolver(UpstreamResolverOptions r)
    {
        var pattern = r.Rule ?? string.Empty;

        var client = r.Block
            ? _defaultClient
            : new UdpDnsClient(new IPEndPoint(IPAddress.Parse(r.Address), r.Port));

        if (string.IsNullOrWhiteSpace(pattern) || pattern is "*" or ".*")
            _fallback.Add(new UpstreamEntry(r.Name, client));

        if (string.IsNullOrWhiteSpace(pattern))
            return;

        var rule = new CompiledRule(pattern, r.Block ? null : client, r.Block, r.Name, null);

        if (IsExact(pattern))
        {
            _exact[pattern.ToLowerInvariant()] = rule;
        }
        else if (IsSuffix(pattern))
        {
            _suffix.Add(pattern[2..], rule);
        }
        else if (IsPrefix(pattern))
        {
            _prefix.Add(ExtractPrefix(pattern), rule);
        }
        else if (IsWildcard(pattern))
        {
            _aho.Add(pattern, rule);
        }
        else
        {
            _regex.Add(rule with
            {
                Regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase)
            });
        }
    }

    private static bool IsExact(string p) =>
        !p.Contains('*') && !p.Contains('^') && !p.Contains('$') &&
        !p.Contains('(') && !p.Contains(')');

    private static bool IsSuffix(string p) =>
        p.StartsWith("*.") && IsExact(p[2..]);

    private static bool IsPrefix(string p) =>
        (p.EndsWith(".*") && !p.StartsWith("*.")) ||
        (p.Contains('*') && !p.StartsWith("*") && !p.EndsWith("*"));

    private static bool IsWildcard(string p) =>
        p.StartsWith("*") && p.EndsWith("*") && p.Length > 2;

    private static string ExtractPrefix(string p) =>
        p.EndsWith(".*") ? p[..^2] : p.TrimEnd('*');

    // ---------------------------------------------------------------------
    // HOSTS + BLOCKLIST LOADING
    // ---------------------------------------------------------------------

    public async Task AddHostsAsync(HostsFileSource src)
    {
        var entries = await src.LoadAsync();

        foreach (var h in entries)
        {
            _hosts.Add(h.Domain, h.Address);
        }
    }

    public async Task AddListAsync(IBlocklistSource source, bool block)
    {
        var parsed = await source.LoadAsync();
        AddRules(parsed, block);
        _aho.Build();
    }

    public void AddRules(IEnumerable<ParsedRule> rules, bool block)
    {
        foreach (var rule in rules)
        {
            var pattern = rule.Pattern.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(pattern))
                continue;

            IDnsClient? client = block ? null : _defaultClient;

            var compiled = new CompiledRule(
                Pattern: pattern,
                Client: client,
                Block: block,
                Name: rule.Source,
                Regex: null);

            if (IsExact(pattern))
            {
                _exact[pattern.ToLowerInvariant()] = compiled;
            }
            else if (IsSuffix(pattern))
            {
                _suffix.Add(pattern[2..], compiled);
            }
            else if (IsPrefix(pattern))
            {
                _prefix.Add(ExtractPrefix(pattern), compiled);
            }
            else if (IsWildcard(pattern))
            {
                _aho.Add(pattern, compiled);
            }
            else
            {
                _regex.Add(compiled with { Regex = rule.Pattern });
            }
        }
    }

    // ---------------------------------------------------------------------
    // MATCHING
    // ---------------------------------------------------------------------

    private RuleResult HostOverride(IPAddress ip) =>
        new RuleResult(
            new List<UpstreamEntry> { new("hosts", new StaticDnsClient(ip)) },
            false);

    public RuleResult Match(string domain, string requestId)
    {
        var lower = domain.ToLowerInvariant();

        // Hosts: most-specific match (exact + wildcard)
        var hostIp = _hosts.MatchMostSpecific(lower);
        if (hostIp != null)
        {
            _logger.LogDebug("Request {RequestId}: Hosts override matched for {Domain}", requestId, domain);
            return HostOverride(hostIp);
        }

        var allow = new List<UpstreamEntry>();
        UpstreamEntry? block = null;

        if (_exact.TryGetValue(lower, out var ex))
            Apply(ex, allow, ref block);

        foreach (var r in _suffix.MatchAll(lower))
            Apply(r, allow, ref block);

        foreach (var r in _prefix.MatchAll(lower))
            Apply(r, allow, ref block);

        foreach (var r in _aho.Match(lower))
            Apply(r, allow, ref block);

        foreach (var r in _regex)
        {
            if (r.Regex != null && r.Regex.IsMatch(domain))
                Apply(r, allow, ref block);
        }

        if (allow.Count > 0)
        {
            _logger.LogDebug("Request {RequestId}: Allow rules matched for {Domain}", requestId, domain);
            return new RuleResult(allow, false);
        }

        if (block != null)
        {
            _logger.LogInformation("Request {RequestId}: Blocking domain {Domain} due to rule {Rule}",
                requestId, domain, block.Name);
            return new RuleResult(new List<UpstreamEntry> { block }, true);
        }

        if (_fallback.Count > 0)
        {
            _logger.LogDebug("Request {RequestId}: Using fallback resolver chain for {Domain}", requestId, domain);
            return new RuleResult(_fallback, false);
        }

        _logger.LogDebug("Request {RequestId}: Using default resolver for {Domain}", requestId, domain);
        return new RuleResult(
            new List<UpstreamEntry> { new("default", _defaultClient) },
            false);
    }

    private void Apply(CompiledRule r, List<UpstreamEntry> allow, ref UpstreamEntry? block)
    {
        if (!r.Block)
        {
            allow.Add(new UpstreamEntry(r.Name, r.Client ?? _defaultClient));
        }
        else
        {
            block ??= new UpstreamEntry(r.Name, r.Client ?? _defaultClient);
        }
    }

    // ---------------------------------------------------------------------
    // QUERY EXECUTION
    // ---------------------------------------------------------------------

    public async Task<byte[]> QueryAsync(string domain, byte[] request, string requestId, CancellationToken ct)
    {
        if (Cache.TryGet(domain, out var cached) && cached != null)
        {
            _logger.LogDebug("Request {RequestId}: Cache HIT for {Domain}", requestId, domain);
            cached[0] = request[0];
            cached[1] = request[1];
            return cached;
        }

        _logger.LogDebug("Request {RequestId}: Cache MISS for {Domain}", requestId, domain);

        var match = Match(domain, requestId);

        if (match.Block)
        {
            _logger.LogInformation("Request {RequestId}: Blocked {Domain} using mode {Mode}",
                requestId, domain, _options.BlockResponse.Mode);

            return BuildBlockResponse(request);
        }

        foreach (var upstream in match.Upstreams)
        {
            try
            {
                _logger.LogDebug("Request {RequestId}: Querying upstream {Upstream} for {Domain}",
                    requestId, upstream.Name, domain);

                var resp = await upstream.Client.QueryAsync(request, ct);

                if (resp.Length < 4)
                    continue;

                var rcode = resp[3] & 0x0F;
                if (rcode == 2)
                {
                    _logger.LogWarning("Request {RequestId}: Upstream {Upstream} returned SERVFAIL for {Domain}",
                        requestId, upstream.Name, domain);
                    continue;
                }

                var copy = resp.ToArray();

                int ttl = ExtractTtl(copy);

                if (ttl > 0)
                {
                    _logger.LogInformation("Request {RequestId}: TTL for {Domain} is {TTL}s (via {Upstream})",
                        requestId, domain, ttl, upstream.Name);

                    ttl = Math.Min(ttl, _options.Caching.TtlSeconds);
                    Cache.Store(domain, copy, TimeSpan.FromSeconds(ttl));
                }
                else
                {
                    _logger.LogWarning("Request {RequestId}: No TTL found for {Domain} (via {Upstream})",
                        requestId, domain, upstream.Name);
                }

                return copy;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Request {RequestId}: Error querying upstream {Upstream} for {Domain}",
                    requestId, upstream.Name, domain);
            }
        }

        _logger.LogError("Request {RequestId}: All upstreams failed for {Domain}, returning SERVFAIL",
            requestId, domain);

        return BuildServfail(request);
    }

    // ---------------------------------------------------------------------
    // TTL EXTRACTION
    // ---------------------------------------------------------------------

    private static int ExtractTtl(byte[] msg)
    {
        int qd = (msg[4] << 8) | msg[5];
        int an = (msg[6] << 8) | msg[7];

        int offset = 12;

        for (int i = 0; i < qd; i++)
            offset = SkipName(msg, offset) + 4;

        int min = int.MaxValue;

        for (int i = 0; i < an; i++)
        {
            offset = SkipName(msg, offset);
            offset += 4;

            int ttl = (msg[offset] << 24) |
                      (msg[offset + 1] << 16) |
                      (msg[offset + 2] << 8) |
                      msg[offset + 3];

            offset += 4;

            int rd = (msg[offset] << 8) | msg[offset + 1];
            offset += 2 + rd;

            if (ttl < min)
                min = ttl;
        }

        return min == int.MaxValue ? -1 : min;
    }

    private static int SkipName(byte[] msg, int offset)
    {
        while (true)
        {
            byte len = msg[offset];

            if (len == 0)
                return offset + 1;

            if ((len & 0xC0) == 0xC0)
                return offset + 2;

            offset += len + 1;
        }
    }

    // ---------------------------------------------------------------------
    // SERVFAIL FALLBACK
    // ---------------------------------------------------------------------

    private static byte[] BuildServfail(byte[] req)
    {
        var r = new List<byte>
        {
            req[0], req[1],
            0x81, 0x82,
            0x00, 0x01,
            0x00, 0x00,
            0x00, 0x00,
            0x00, 0x00
        };

        r.AddRange(req.Skip(12));
        return r.ToArray();
    }
}

