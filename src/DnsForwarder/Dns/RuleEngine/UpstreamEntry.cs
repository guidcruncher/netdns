using DnsForwarder.Dns.Core;

namespace DnsForwarder.Dns.RuleEngine;

public sealed record UpstreamEntry(string Name, IDnsClient Client);
