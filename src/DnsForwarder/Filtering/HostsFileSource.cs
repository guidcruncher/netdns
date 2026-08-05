using System.Net;

namespace DnsForwarder.Filtering;

public sealed class HostsFileSource
{
    private readonly IEnumerable<string> _paths;

    public HostsFileSource(IEnumerable<string> paths)
    {
        _paths = paths;
    }

    public async Task<IEnumerable<HostsEntry>> LoadAsync()
    {
        var list = new List<HostsEntry>();

        foreach (var path in _paths)
        {
            if (!File.Exists(path))
                continue;

            var lines = await File.ReadAllLinesAsync(path);

            foreach (var raw in lines)
            {
                var line = raw.Trim();

                // Skip empty lines
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Skip full-line comments
                if (line.StartsWith("#"))
                    continue;

                // Remove inline comments
                var hashIndex = line.IndexOf('#');
                if (hashIndex >= 0)
                    line = line[..hashIndex].Trim();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Split into tokens
                var parts = line.Split(' ', '\t', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                    continue;

                if (!IPAddress.TryParse(parts[0], out var ip))
                    continue;

                // Each remaining token is a hostname
                for (int i = 1; i < parts.Length; i++)
                {
                    list.Add(new HostsEntry
                    {
                        Domain = parts[i].Trim().ToLowerInvariant(),
                        Address = ip,
                        Source = path
                    });
                }
            }
        }

        return list;
    }
}
