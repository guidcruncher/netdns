namespace DnsForwarder.RuleEngine;

public sealed record UpstreamEntry(string Name, IDnsClient Client);
