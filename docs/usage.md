# Usage

## Quick start (local)
1. Build and run:
   - dotnet run --project src/DnsForwarder/DnsForwarder.csproj -- --config src/DnsForwarder/appsettings.Development.json

2. Run in Docker:
   - docker build -t dnsforwarder .
   - docker run -p 53:53/udp -p 1080:1080 dnsforwarder

3. Use dig to test:
   - dig @127.0.0.1 -p 1053 example.com

## Configuration (appsettings.json)
Example snippet:

```json
{
  "Resolvers": [
    {
      "Name": "Cloudflare",
      "Address": "1.1.1.1",
      "Port": 53,
      "Block": false
    },
    {
      "Name": "BlockAds",
      "Rule": "^(ads|tracking)\\.",
      "Block": true
    }
  ],
  "DefaultResolvers": [
    { "Name": "Cloudflare", "Address": "1.1.1.1", "Port": 53 }
  ],
  "Caching": {
    "Enabled": true,
    "TtlSeconds": 300,
    "MaxEntries": 10000
  }
}
```

## Blocklist sources
- Local file list: implement FileBlocklistSource with paths in configuration.
- URL lists: UrlBlocklistSource caches remote blocklists under `blocklist-cache/`.

## Metrics & Logging
- Prometheus-style metrics are exposed at: `http://127.0.0.1:1080/metrics` (example).
- Structured logging includes a `RequestId` for tracing individual DNS requests.
