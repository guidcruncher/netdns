using System.Text.RegularExpressions;

using DnsForwarder.Dns.Filtering;

namespace DnsForwarder.Dns.Tests;

public sealed class InlineBlocklistSource : IBlocklistSource
{
    private readonly IEnumerable<string> _items;

    public InlineBlocklistSource(IEnumerable<string> items)
    {
        _items = items;
    }

    public Task<IEnumerable<ParsedRule>> LoadAsync()
    {
        var rules = _items.Select(i => new ParsedRule
        {
            Raw = i,
            Source = "inline",
            Pattern = new Regex($"^{Regex.Escape(i)}$", RegexOptions.IgnoreCase)
        });

        return Task.FromResult(rules);
    }
}
