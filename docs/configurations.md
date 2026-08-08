# Configuration examples

Here are full example configuration files you can use as a starting point when running DnsForwarder.

They are available in the project root.

## Example

Comprehensive example for local/dev running.

```json
{
  "Dns": {
    "Listen": {
      "Address": "127.0.0.1",
      "Port": 1053
    },
    "DefaultResolvers": [{
      "Name": "Cloudflare",
      "Address": "1.1.1.1",
      "Port": 53
    }],
    "Resolvers": [
      {
        "Name": "LocalDNS",
        "Address": "127.0.0.1",
        "Port": 5353,
        "Rule": "^localdev\\.",
        "Block": false
      },
      {
        "Name": "BlockDevAds",
        "Rule": "^(ads|tracking)\\.",
        "Block": true
      }
    ],
    "HostsFiles": [],
    "Caching": {
      "Enabled": true,
      "TtlSeconds": 300,
      "MaxEntries": 2000
    },
    "BlockResponse": {
      "Mode": "NXDOMAIN",
      "StaticIp": "0.0.0.0",
      "Ttl": 60
    }
  },
  "Dhcp": {
    "Enabled": false,
    "ListenAddress": "127.0.0.1",
    "ListenPort": 1067,
    "LeaseStorePath": "leases.json",
    "PoolCidr": "192.168.10.0/24",

    "ServerIdentifier": "192.168.10.1",
    "Router": "192.168.10.1",
    "DnsServer": "1.1.1.1",
    "NtpServer": "",

    "LeaseHours": 1,

    "ArpTimeoutMs": 500,

    "BadIpStorePath": "badips.json"
  },
  "Ntp": {
    "Enabled": true,
    "ListenAddress": "127.0.0.1",
    "Port": 1123,
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

## Docker

Example tuned for running inside Docker (paths reference `/app/blocklists` for mounted blocklists and uses an alternate DNS port 1053 to avoid requiring root on the host).

```json
{
  "Dns": {
    "Listen": {
      "Address": "0.0.0.0",
      "Port": 53
    },
    "DefaultResolvers": [{
      "Name": "Cloudflare",
      "Address": "1.1.1.1",
      "Port": 53
    }],
    "Resolvers": [
      {
        "Name": "DockerInternal",
        "Address": "127.0.0.11",
        "Port": 53,
        "Rule": "^docker\\.",
        "Block": false
      },
      {
        "Name": "BlockAds",
        "Rule": "^(ads|tracking|metrics)\\.",
        "Block": true
      }
    ],
    "HostsFiles": [],
    "Caching": {
      "Enabled": true,
      "TtlSeconds": 300,
      "MaxEntries": 10000
    },
    "BlockResponse": {
      "Mode": "NXDOMAIN",
      "StaticIp": "0.0.0.0",
      "Ttl": 60
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
    "Level": "Warning"
  }
}
```

### Prometheus

Metrics are exposed at `http://localhost:1080/metrics` and that Prometheus (if enabled in compose) discovers the `dnsforwarder` target.
