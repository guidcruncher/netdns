# Architecture & Flow Diagrams

Below are simple mermaid diagrams illustrating the high-level architecture and request flows for DNS, DHCP and NTP.

## High-level component diagram

```mermaid
flowchart LR
  Client[Client]
  DNSF[DnsForwarder]
  Cache[DnsCache]
  Rules[RuleEngine]
  Upstreams[Upstream Resolvers]
  Blocklists[Blocklist Sources]
  Metrics[Metrics / EventBus]

  Client -->|DNS/DHCP/NTP| DNSF
  DNSF --> Rules
  Rules --> Blocklists
  DNSF --> Cache
  DNSF --> Upstreams
  DNSF --> Metrics
```

## DNS request sequence

```mermaid
sequenceDiagram
  participant C as Client
  participant D as DnsForwarder
  participant R as RuleEngine
  participant Cg as Cache
  participant U as Upstream

  C->>D: UDP DNS query
  D->>R: Match(domain)
  alt blocked
    R-->>D: Block
    D->>C: NXDOMAIN (BuildBlockedResponse)
  else cache hit
    R-->>D: Upstream list
    D->>Cg: TryGetPooled(domain)
    Cg-->>D: pooled buffer
    D->>C: Response (patched ID)
  else forward
    D->>U: Forward
    U-->>D: Response
    D->>Cg: Store(domain, response)
    D->>C: Response (patched ID)
  end
```

## DHCP flow (simplified)

```mermaid
sequenceDiagram
  participant Client
  participant DnsF as DnsForwarder (DHCP)
  participant Alloc as CidrPoolAllocator

  Client->>DnsF: DHCP DISCOVER
  DnsF->>Alloc: Allocate(used)
  Alloc-->>DnsF: IP (or null)
  DnsF->>Client: DHCP OFFER (with allocated IP)
  Client->>DnsF: DHCP REQUEST
  DnsF->>Client: DHCP ACK
```

## NTP flow (simplified)

```mermaid
sequenceDiagram
  participant Client
  participant DnsF as DnsForwarder (NTP)

  Client->>DnsF: NTP request (UDP/123)
  DnsF->>DnsF: Build NTP response
  DnsF-->>Client: NTP response
```
