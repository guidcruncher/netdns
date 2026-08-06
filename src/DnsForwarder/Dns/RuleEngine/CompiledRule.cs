using System.Text.RegularExpressions;

using DnsForwarder.Dns.Core;

namespace DnsForwarder.Dns.RuleEngine;

public sealed record CompiledRule(
    string Pattern,
    IDnsClient? Client,
    bool Block,
    string Name,
    Regex? Regex);
