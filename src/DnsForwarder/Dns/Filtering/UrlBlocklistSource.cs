using System.Security.Cryptography;
using System.Text;

namespace DnsForwarder.Dns.Filtering;

public sealed class UrlBlocklistSource : IBlocklistSource
{
    private readonly IEnumerable<string> _urls;
    private readonly HttpClient _http = new();
    private readonly string _cacheDir;
    private readonly TimeSpan _ttl = TimeSpan.FromHours(24);

    public UrlBlocklistSource(IEnumerable<string> urls)
    {
        _urls = urls;
        _cacheDir = Path.Combine(AppContext.BaseDirectory, "blocklist-cache");
        Directory.CreateDirectory(_cacheDir);
    }

    public async Task<IEnumerable<ParsedRule>> LoadAsync()
    {
        var results = new List<ParsedRule>();

        foreach (var url in _urls)
        {
            var text = await LoadWithCacheAsync(url);

            foreach (var line in text.Split('\n'))
            {
                var parsed = AdGuardRuleParser.Parse(line, url);
                if (parsed != null)
                    results.Add(parsed);
            }
        }

        return results;
    }

    private async Task<string> LoadWithCacheAsync(string url)
    {
        var cachePath = CachePath(url);

        if (File.Exists(cachePath))
        {
            var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(cachePath);
            if (age < _ttl)
                return await File.ReadAllTextAsync(cachePath);
        }

        try
        {
            var text = await _http.GetStringAsync(url);
            await File.WriteAllTextAsync(cachePath, text);
            return text;
        }
        catch
        {
            if (File.Exists(cachePath))
                return await File.ReadAllTextAsync(cachePath);

            return string.Empty;
        }
    }

    private string CachePath(string url)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(url));
        var hex = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        return Path.Combine(_cacheDir, $"{hex}.txt");
    }
}
