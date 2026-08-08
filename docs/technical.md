# Technical Overview

## Components
- RuleEngine
  - Evaluates host/rule patterns and selects upstreams or block actions.
  - Supports exact, prefix, suffix, substring and regex rules.

- HostMatcher
  - Resolves host overrides and wildcard matching (Longest-core-wins).

- DnsCache (Dns/RuleEngine/DnsCache.cs)
  - Two access patterns:
    - TryGet: returns a copy of the cached response (safe for callers).
    - TryGetPooled: returns pooled buffer with length — caller must not return the buffer to the pool.
  - Uses ArrayPool<byte>.Shared to reduce allocations.

- PooledBuffer (Dns/Core/PooledBuffer.cs)
  - Small wrapper for a byte[] with a Return() method for pooled buffers.

- DnsParser
  - Parses DNS wire format and builds responses (including BuildBlockedResponse).
  - Internal helpers: ReadName, WriteNameWire, GetNameWireLength.

- EventBus
  - Bounded channel-based event publisher/consumer for low-overhead events.

## Data flow for a query
1. UDP request received and parsed by DnsParser.
2. RuleEngine.Match determines upstream(s) and block status.
3. If blocked → BuildBlockedResponse and return.
4. If cached (TryGetPooled) → clone/patch ID or rent send buffer and return PooledBuffer(fromPool: true).
5. Otherwise forward via QueryExecutor/IDnsClient, update cache (Store), and respond.

## Caching strategy
- Cache entries store pooled buffers (Buffer + Length + Expires).
- Storing: leases an ArrayPool buffer and copies response bytes.
- Eviction: entries removed on expiry; pooled buffers are returned to ArrayPool.

## Concurrency & perf
- Uses ConcurrentDictionary and ConcurrentBag (ListPool) for low-contention pooling.
- EventBus uses Channel with a bounded capacity; drop policy for full channel.
- Use ArrayPool to reduce GC pressure under high QPS.

## Extensibility
- Blocklist sources implement IBlocklistSource (FileBlocklistSource, UrlBlocklistSource).
- Metrics through IDnsMetrics interface; additional metrics can be added here.
