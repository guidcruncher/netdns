using DnsForwarder.Dns.Core;

namespace DnsForwarder.Dns.RuleEngine;

// Make UpstreamEntry a readonly value type to avoid heap allocations per match
public readonly record struct UpstreamEntry(string Name, IDnsClient Client);
