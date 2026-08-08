using System.Net;
using System.Text.RegularExpressions;

using DnsForwarder.Dns.Core;
using DnsForwarder.Dns.Filtering;
using DnsForwarder.Utils;

using Microsoft.Extensions.Logging;

namespace DnsForwarder.Dns.RuleEngine;


internal sealed class RuleCompiler
{
    private readonly ILogger _logger;
    private readonly DnsForwarderOptions _options;

    public IDnsClient DefaultClient { get; }
    public HostMatcher Hosts { get; } = new();

    public Dictionary<string, CompiledRule> Exact { get; } = new(StringComparer.OrdinalIgnoreCase);
    public SuffixTrie Suffix { get; } = new();
    public PrefixTrie Prefix { get; } = new();
    public AhoCorasickMatcher<CompiledRule> Aho { get; } = new();
    public List<CompiledRule> RegexRules { get; } = new();
    public List<UpstreamEntry> FallbackResolvers { get; } = new();

    public RuleCompiler(DnsForwarderOptions options, ILogger logger)
    {
        _options = options;
        _logger = logger;

        UpstreamResolverOptions selected;

        if (options.DefaultResolvers != null && options.DefaultResolvers.Count > 0)
        {
            selected = options.DefaultResolvers[0];
        }
        else
        {
            selected = new UpstreamResolverOptions
            {
                Address = "8.8.8.8",
                Port = 53,
                Name = "fallback-default"
            };
        }

        DefaultClient = new UdpDnsClient(
            new IPEndPoint(IPAddress.Parse(selected.Address), selected.Port));
    }

    public void AddResolver(UpstreamResolverOptions r)
    {
        var pattern = r.Rule ?? string.Empty;

        var client = r.Block
            ? DefaultClient
            : new UdpDnsClient(new IPEndPoint(IPAddress.Parse(r.Address), r.Port));

        if (string.IsNullOrWhiteSpace(pattern) || pattern is "*" or ".*")
            FallbackResolvers.Add(new UpstreamEntry(r.Name, client));

        if (string.IsNullOrWhiteSpace(pattern))
            return;

        var rule = new CompiledRule(pattern, r.Block ? null : client, r.Block, r.Name, null);

        if (IsExact(pattern))
        {
            Exact[pattern.ToLowerInvariant()] = rule;
        }
        else if (IsSuffix(pattern))
        {
            Suffix.Add(pattern[2..], rule);
        }
        else if (IsPrefix(pattern))
        {
            Prefix.Add(ExtractPrefix(pattern), rule);
        }
        else if (IsWildcard(pattern))
        {
            Aho.Add(pattern, rule);
        }
        else
        {
            RegexRules.Add(rule with
            {
                Regex = new Regex(pattern, RegexOptions.IgnoreCase)
            });
        }
    }

    public void AddRules(IEnumerable<ParsedRule> rules, bool block)
    {
        foreach (var rule in rules)
        {
            var pattern = rule.Pattern.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(pattern))
                continue;

            IDnsClient? client = block ? null : DefaultClient;

            var compiled = new CompiledRule(
                Pattern: pattern,
                Client: client,
                Block: block,
                Name: rule.Source,
                Regex: null);

            if (IsExact(pattern))
            {
                Exact[pattern.ToLowerInvariant()] = compiled;
            }
            else if (IsSuffix(pattern))
            {
                Suffix.Add(pattern[2..], compiled);
            }
            else if (IsPrefix(pattern))
            {
                Prefix.Add(ExtractPrefix(pattern), compiled);
            }
            else if (IsWildcard(pattern))
            {
                Aho.Add(pattern, compiled);
            }
            else
            {
                RegexRules.Add(compiled with { Regex = rule.Pattern });
            }
        }
    }

    public void BuildAutomata()
    {
        Aho.Build();
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
}
