using System;
using System.Buffers;
using System.Collections.Concurrent;

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
                // Copy only the used length to avoid exposing pooled buffer and keep callers independent
                var copy = new byte[entry.Length];
                Buffer.BlockCopy(entry.Buffer, 0, copy, 0, entry.Length);
                response = copy;
                return true;
            }

            // Entry expired — remove and return pooled buffer
            if (_entries.TryRemove(domain, out var removed))
            {
                ArrayPool<byte>.Shared.Return(removed.Buffer, clearArray: true);
            }
        }

        return false;
    }

    public void Store(string domain, byte[] response, TimeSpan ttl)
    {
        var expires = DateTime.UtcNow + ttl;
        var pool = ArrayPool<byte>.Shared;
        var buf = pool.Rent(response.Length);
        Buffer.BlockCopy(response, 0, buf, 0, response.Length);

        var newEntry = new CacheEntry(buf, response.Length, expires);

        _entries.AddOrUpdate(domain,
            newEntry,
            (key, existing) =>
            {
                // Return previous buffer to pool before replacing
                ArrayPool<byte>.Shared.Return(existing.Buffer, clearArray: true);
                return newEntry;
            });
    }

    // Try to return the pooled cached buffer directly (caller must not return it to the pool).
    public bool TryGetPooled(string domain, out byte[]? buffer, out int length)
    {
        buffer = null;
        length = 0;

        if (_entries.TryGetValue(domain, out var entry))
        {
            if (DateTime.UtcNow < entry.Expires)
            {
                buffer = entry.Buffer;
                length = entry.Length;
                return true;
            }

            if (_entries.TryRemove(domain, out var removed))
            {
                ArrayPool<byte>.Shared.Return(removed.Buffer, clearArray: true);
            }
        }

        return false;
    }

    private sealed class CacheEntry
    {
        public byte[] Buffer { get; }
        public int Length { get; }
        public DateTime Expires { get; }

        public CacheEntry(byte[] buffer, int length, DateTime expires)
        {
            Buffer = buffer;
            Length = length;
            Expires = expires;
        }
    }
}
