using System.Net;
using System.Text.RegularExpressions;

using DnsForwarder.Dns.Core;
using DnsForwarder.Dns.Filtering;
using DnsForwarder.Utils;

using Microsoft.Extensions.Logging;

namespace DnsForwarder.Dns.RuleEngine;

public sealed class RuleEngine
{
    private readonly ILogger<RuleEngine> _logger;
    private readonly DnsForwarderOptions _options;

    private readonly RuleCompiler _compiler;
    private readonly RuleMatcher _matcher;
    private readonly ResolverChainBuilder _chainBuilder;
    private readonly QueryExecutor _executor;
    private readonly BlockResponseBuilder _blockBuilder;

    public DnsCache Cache { get; } = new();

    public RuleEngine(DnsForwarderOptions options, ILogger<RuleEngine> logger)
    {
        _options = options;
        _logger = logger;

        _compiler = new RuleCompiler(options, logger);
        _matcher = new RuleMatcher(_compiler, logger);
        _chainBuilder = new ResolverChainBuilder(options, _compiler.DefaultClient, _compiler.FallbackResolvers, logger);
        _blockBuilder = new BlockResponseBuilder(options);
        _executor = new QueryExecutor(Cache, logger);

        if (options.Resolvers != null)
        {
            foreach (var r in options.Resolvers)
                _compiler.AddResolver(r);
        }

        _compiler.BuildAutomata();
    }

    public async Task AddHostsAsync(HostsFileSource src)
    {
        var entries = await src.LoadAsync();

        foreach (var h in entries)
            _compiler.Hosts.Add(h.Domain, h.Address);
    }

    public async Task AddListAsync(IBlocklistSource source, bool block)
    {
        var parsed = await source.LoadAsync();
        _compiler.AddRules(parsed, block);
        _compiler.BuildAutomata();
    }

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

        var match = _matcher.Match(domain, requestId);

        if (match.Block)
        {
            _logger.LogInformation("Request {RequestId}: Blocked {Domain} using mode {Mode}",
                requestId, domain, _options.BlockResponse.Mode);

            return _blockBuilder.BuildBlockResponse(request);
        }

        var upstreams = _chainBuilder.BuildChain(match, domain, requestId);

        var response = await _executor.ExecuteAsync(upstreams, domain, request, requestId, ct);

        if (response == null)
        {
            _logger.LogError("Request {RequestId}: All upstreams failed for {Domain}, returning SERVFAIL",
                requestId, domain);

            return BlockResponseBuilder.BuildServfail(request);
        }

        return response;
    }

    public RuleResult Match(string domain, string requestId)
    {
        return _matcher.Match(domain, requestId);
    }

    public void AddRules(IEnumerable<ParsedRule> rules, bool block)
    {
        _compiler.AddRules(rules, block);
        _compiler.BuildAutomata();
    }


}
