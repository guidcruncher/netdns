using System.Text.RegularExpressions;

namespace DnsForwarder.Filtering;

public sealed class ParsedRule
{
    public required string Source { get; init; }
    public required string Raw { get; init; }
    public required Regex Pattern { get; init; }

}
