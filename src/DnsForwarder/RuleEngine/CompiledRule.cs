using System.Text.RegularExpressions;

namespace DnsForwarder.RuleEngine;

public sealed record CompiledRule(
    string Pattern,
    IDnsClient? Client,
    bool Block,
    string Name,
    Regex? Regex);
