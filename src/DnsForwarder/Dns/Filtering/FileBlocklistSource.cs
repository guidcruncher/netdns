namespace DnsForwarder.Dns.Filtering;

public sealed class FileBlocklistSource : IBlocklistSource
{
    private readonly IEnumerable<string> _paths;

    public FileBlocklistSource(IEnumerable<string> paths)
    {
        _paths = paths;
    }

    public async Task<IEnumerable<ParsedRule>> LoadAsync()
    {
        var results = new List<ParsedRule>();

        foreach (var path in _paths)
        {
            if (!File.Exists(path))
                continue;

            var text = await File.ReadAllTextAsync(path);

            foreach (var line in text.Split('\n'))
            {
                var parsed = AdGuardRuleParser.Parse(line, path);
                if (parsed != null)
                    results.Add(parsed);
            }
        }

        return results;
    }
}
