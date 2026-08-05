namespace DnsForwarder.Filtering;

public sealed class CompositeBlocklistSource : IBlocklistSource
{
    private readonly IEnumerable<IBlocklistSource> _sources;

    public CompositeBlocklistSource(IEnumerable<IBlocklistSource> sources)
    {
        _sources = sources;
    }

    public async Task<IEnumerable<ParsedRule>> LoadAsync()
    {
        var all = new List<ParsedRule>();

        foreach (var src in _sources)
        {
            var rules = await src.LoadAsync();
            all.AddRange(rules);
        }

        return all;
    }
}
