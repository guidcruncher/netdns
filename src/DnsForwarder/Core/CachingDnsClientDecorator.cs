using System.Collections.Concurrent;

namespace DnsForwarder;

public sealed class CachingDnsClientDecorator : IDnsClient
{
    private readonly IDnsClient _inner;
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly int _maxEntries;

    public CachingDnsClientDecorator(IDnsClient inner, int maxEntries)
    {
        _inner = inner;
        _maxEntries = maxEntries;
    }

    public async Task<byte[]> QueryAsync(byte[] request, CancellationToken ct)
    {
        var msg = DnsParser.Parse(request);
        var q = msg.Questions.First();
        var key = $"{q.Name}|{q.Type}";

        if (_cache.TryGetValue(key, out var entry) && entry.ExpiresAt > DateTimeOffset.UtcNow)
        {
            return entry.Response;
        }

        var response = await _inner.QueryAsync(request, ct);
        var respMsg = DnsParser.Parse(response);
        var ttl = respMsg.GetMinTtl();
        var expires = DateTimeOffset.UtcNow.AddSeconds(ttl);

        if (_cache.Count < _maxEntries)
        {
            _cache[key] = new CacheEntry(response, expires);
        }

        return response;
    }

    private sealed record CacheEntry(byte[] Response, DateTimeOffset ExpiresAt);
}
