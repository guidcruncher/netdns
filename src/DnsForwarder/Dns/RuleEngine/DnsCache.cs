using System.Collections.Concurrent;
using System.Net;
using System.Text.RegularExpressions;

using DnsForwarder.Dns.Filtering;

namespace DnsForwarder.Dns.RuleEngine;

public sealed class DnsCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _entries = new();

    public bool TryGet(string domain, out byte[]? response)
    {
        response = null;

        if (_entries.TryGetValue(domain, out var entry))
        {
            if (DateTime.UtcNow < entry.Expires)
            {
                response = entry.Response;
                return true;
            }

            _entries.TryRemove(domain, out _);
        }

        return false;
    }

    public void Store(string domain, byte[] response, TimeSpan ttl)
    {
        var expires = DateTime.UtcNow + ttl;
        _entries[domain] = new CacheEntry(response, expires);
    }

    private sealed record CacheEntry(byte[] Response, DateTime Expires);
}
