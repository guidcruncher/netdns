# Examples

## Example: Block a domain via blocklist file
1. Create `/etc/dnsforwarder/blocklist.txt` with:
```
! comment lines are ignored
ads.example.com
tracking.example.com
```
2. Configure FileBlocklistSource paths in startup configuration:
```json
"BlocklistSources": {
  "Files": [ "/etc/dnsforwarder/blocklist.txt" ]
}
```

## Example: Start the service (development)
- From repo root:
  - dotnet run --project src/DnsForwarder/DnsForwarder.csproj -- --config src/DnsForwarder/appsettings.Development.json

## Example: Start using docker-compose
- From repo root:
  - docker-compose -f docs/docker-compose.yml up --build

## Example: Query & verify NXDOMAIN blocked response
- After configuring a rule that blocks `ads.example.com`:
  - dig @127.0.0.1 -p 1053 ads.example.com
  - The response will have RCODE=3 (NXDOMAIN). Unit tests include a BuildBlockedResponse test.

## Example: Benchmark run
- dotnet run -c Release --project tests/DnsForwarder.Benchmarks/DnsForwarder.Benchmarks.csproj

## Troubleshooting tips
- If caching appears inconsistent, verify system time and TTL configuration.
- For blocklist download failures, UrlBlocklistSource will return cached copy if available.
