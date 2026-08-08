# Technical Overview

## Components
- DNS Service
  - Responsible for parsing incoming DNS wire requests, applying rule matching and blocklists, caching responses, and forwarding to upstream resolvers.
  - Key files: `Dns/Core/DnsForwarderService.cs`, `Dns/Core/DnsParser.cs`, `Dns/RuleEngine/DnsCache.cs`.

- DHCP Service
  - Experimental DHCP allocator and packet builders/parsers; supports building and parsing DHCP packets for testing and prototype DHCP responses.
  - Key files: `Dhcp/CidrPoolAllocator.cs`, `Dhcp/*` (packet codecs and helpers under `src/DnsForwarder/Dhcp`).
  - Ports: typical DHCP server port is UDP 67 (server listening). The service may also generate client-side packets for tests.

- NTP Service
  - Experimental NTP responder/experiment code used to exercise NTP response handling.
  - Key files: `Ntp/*` under `src/DnsForwarder/Ntp` (see repo tree for exact filenames).
  - Ports: NTP uses UDP port 123.

- RuleEngine
  - Evaluates host/rule patterns and selects upstreams or block actions.
  - Hosts support wildcard matching with the "longest-core-wins" rule.

- DnsCache
  - Two access patterns:
    - TryGet: returns a copy of the cached response (safe for callers).
    - TryGetPooled: returns pooled buffer with length — caller must not return the buffer to the pool.
  - Uses `ArrayPool<byte>.Shared` to reduce allocations and GC pressure.

- PooledBuffer
  - Small wrapper for a byte[] with a Return() method for pooled buffers (`Dns/Core/PooledBuffer.cs`).

- Blocklist sources
  - `FileBlocklistSource` — loads local files line-by-line and parses with `AdGuardRuleParser`.
  - `UrlBlocklistSource` — downloads remote blocklists and caches them under `blocklist-cache/`, with a TTL.

- EventBus
  - Bounded channel-based event publisher/consumer for low-overhead events.

## Data flow (detailed)
DNS request flow:
1. UDP packet arrives and is passed to `DnsForwarderService.ProcessAsync`.
2. `DnsParser.Parse` converts wire bytes to `DnsMessage` (questions, flags, etc.).
3. `RuleEngine.Match` evaluates block rules, host overrides, and upstream selection.
4. If blocked → `DnsParser.BuildBlockedResponse` is used to craft an NXDOMAIN response and returned as a non-pooled `PooledBuffer`.
5. If present in cache → `DnsCache.TryGetPooled` may return a pooled buffer; the service clones or rents a send buffer, patches the DNS ID and returns a `PooledBuffer` with `fromPool: true`.
6. Otherwise forward to upstream resolvers via `QueryExecutor` / `IDnsClient`. On response store in cache via `DnsCache.Store` and return to client.

DHCP flow (overview):
- DHCP helper code constructs and parses packets for DISCOVER/OFFER/REQUEST/ACK flows. The `CidrPoolAllocator` is used to select an available IP within a configurable CIDR when crafting lease offers.

NTP flow (overview):
- NTP experimental code crafts and parses NTP packets to respond to NTP client queries. The NTP handling is experimental and intended primarily for lab/test usage.

## Caching strategy
- Cache entries store pooled buffers (Buffer + Length + Expires).
- Storing: rents an `ArrayPool<byte>` buffer, copies response bytes, creates `CacheEntry` and stores it in a `ConcurrentDictionary`.
- Eviction: when `TryGet` or `TryGetPooled` observes an expired entry it removes it and returns the pooled buffer to the pool.

## Concurrency & performance notes
- Uses `ConcurrentDictionary` and `ConcurrentBag` (`ListPool`) for low-contention pooling.
- EventBus uses `Channel` with bounded capacity and `DropWrite` policy to avoid blocking producers under high load.
- ArrayPool usage reduces heap allocations for frequent short-lived UDP responses.

## Observability
- Metrics interface `IDnsMetrics` collects counters for cache hits/misses, forwarded queries, blocked queries, etc.
- Logs include structured fields such as `RequestId`, remote endpoint, domain, and upstream used.

## Extensibility and integration
- Blocklist sources implement `IBlocklistSource` and can be added via configuration.
- Metrics may be augmented by implementing `IDnsMetrics` and registering in DI.
- Add new upstream resolver types by implementing `IDnsClient`.
