using System.Net;
using System.Text.RegularExpressions;

using DnsForwarder.Dns.Core;
using DnsForwarder.Dns.Filtering;
using DnsForwarder.Utils;

using Microsoft.Extensions.Logging;

namespace DnsForwarder.Dns.RuleEngine;


internal sealed class RuleMatcher
{
    private readonly RuleCompiler _compiler;
    private readonly ILogger _logger;

    public RuleMatcher(RuleCompiler compiler, ILogger logger)
    {
        _compiler = compiler;
        _logger = logger;
    }

    private RuleResult HostOverride(IPAddress ip) =>
        new RuleResult(
            new List<UpstreamEntry> { new("hosts", new StaticDnsClient(ip)) },
            false);

    public RuleResult Match(string domain, string requestId)
    {
        var lower = domain.ToLowerInvariant();

        var hostIp = _compiler.Hosts.MatchMostSpecific(lower);
        if (hostIp != null)
        {
            _logger.LogDebug("Request {RequestId}: Hosts override matched for {Domain}", requestId, domain);
            return HostOverride(hostIp);
        }

        var allow = ListPool<UpstreamEntry>.Rent();
        UpstreamEntry? block = null;

        if (_compiler.Exact.TryGetValue(lower, out var ex))
            Apply(ex, allow, ref block);

        foreach (var r in _compiler.Suffix.MatchAll(lower))
            Apply(r, allow, ref block);

        foreach (var r in _compiler.Prefix.MatchAll(lower))
            Apply(r, allow, ref block);

        foreach (var r in _compiler.Aho.Match(lower))
            Apply(r, allow, ref block);

        foreach (var r in _compiler.RegexRules)
        {
            if (r.Regex != null && r.Regex.IsMatch(domain))
                Apply(r, allow, ref block);
        }

        if (allow.Count > 0)
        {
            _logger.LogDebug("Request {RequestId}: Allow rules matched for {Domain}", requestId, domain);
            var resultList = new List<UpstreamEntry>(allow);
            ListPool<UpstreamEntry>.Return(allow);
            return new RuleResult(resultList, false);
        }

        if (block != null)
        {
            _logger.LogInformation("Request {RequestId}: Blocking domain {Domain} due to rule {Rule}",
                requestId, domain, block.Value.Name);
            ListPool<UpstreamEntry>.Return(allow);
            return new RuleResult(new List<UpstreamEntry> { block.Value }, true);
        }

        ListPool<UpstreamEntry>.Return(allow);
        return new RuleResult(new List<UpstreamEntry>(), false);
    }

    private void Apply(CompiledRule r, List<UpstreamEntry> allow, ref UpstreamEntry? block)
    {
        if (!r.Block)
        {
            allow.Add(new UpstreamEntry(r.Name, r.Client ?? _compiler.DefaultClient));
        }
        else
        {
            block ??= new UpstreamEntry(r.Name, r.Client ?? _compiler.DefaultClient);
        }
    }
}
