# Usage

## Quick start (local)
1. Build and run:
   - dotnet run --project src/DnsForwarder/DnsForwarder.csproj -- --config src/DnsForwarder/appsettings.Development.json

2. Run in Docker (single container):
   - docker build -t dnsforwarder .
   - docker run -p 53:53/udp -p 1080:1080 dnsforwarder -- --config /app/appsettings.Docker.json

3. Run with docker-compose (recommended for multi-protocol exposure):
   - docker-compose -f docs/docker-compose.yml up --build

4. Use dig to test:
   - dig @127.0.0.1 -p 1053 example.com

## Configuration (appsettings.json)

```json
{
  "Dns": {
    "Listen": {
      "Address": "0.0.0.0",
      "Port": 5353
    },
    "DefaultResolvers": [{
      "Name": "Cloudflare",
      "Address": "1.1.1.1",
      "Port": 53
    }],
    "Resolvers": [
      {
        "Name": "InternalDNS",
        "Address": "10.0.0.10",
        "Port": 53,
        "Rule": "^(.+\\.corp\\.local)$",
        "Block": false
      },
      {
        "Name": "BlockTracking",
        "Rule": "^(tracking\\.|ads\\.).*",
        "Block": true
      },
      {
        "Name": "GoogleDNS",
        "Address": "8.8.8.8",
        "Port": 53,
        "Rule": "^google\\.com$",
        "Block": false
      }
    ],
    "HostsFiles": [],
    "BlockResponse": {
      "Mode": "NXDOMAIN",
      "StaticIp": "0.0.0.0",
      "Ttl": 60
    },
    "Caching": {
      "Enabled": true,
      "TtlSeconds": 300,
      "MaxEntries": 10000
    }
  },
  "Dhcp": {
    "Enabled": true,
    "ListenAddress": "0.0.0.0",
    "ListenPort": 67,
    "LeaseStorePath": "/var/lib/dnsforwarder/leases.json",
    "PoolCidr": "192.168.10.0/24",

    "ServerIdentifier": "192.168.10.1",
    "Router": "192.168.10.1",
    "DnsServer": "1.1.1.1",
    "NtpServer": "",

    "LeaseHours": 1,

    "ArpTimeoutMs": 500,

    "BadIpStorePath": "/var/lib/dnsforwarder/badips.json"
  },
  "Ntp": {
    "Enabled": true,
    "ListenAddress": "0.0.0.0",
    "Port": 123,
    "BufferSize": 65536,
    "Stratum": 1,
    "ReferenceId": "LOCL",
    "Upstream": {
      "Enabled": true,
      "Servers": [
        "0.pool.ntp.org",
        "1.pool.ntp.org"
      ],
      "PollIntervalSeconds": 16
    }
  },
  "Metrics": {
    "Enabled": true,
    "StorageEngine": "prometheus",
    "Location": "/metrics",
    "ListenAddress": "127.0.0.1",
    "ListenPort": 1080
  },
  "Logging": {
    "Level": "Debug"
  }
}
```

## Blocklist sources
- Local file list: implement FileBlocklistSource with paths in configuration.
- URL lists: UrlBlocklistSource caches remote blocklists under `blocklist-cache/`.

## Exposed ports in the Docker Compose example
- DNS (UDP) — 53
- DHCP (UDP) — 67 (server), 68 (client) — only relevant if DHCP mode is enabled
- NTP (UDP) — 123
- Metrics (HTTP) — 1080

## Metrics & Logging
- Prometheus-style metrics are exposed at: `http://127.0.0.1:1080/metrics` (example).
- Structured logging includes a `RequestId` for tracing individual DNS requests.
