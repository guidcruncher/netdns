namespace DnsForwarder.Dns.RuleEngine;

public sealed record RuleResult(List<UpstreamEntry> Upstreams, bool Block);
