# Configuration examples

This folder contains full example configuration files you can use as a starting point when running DnsForwarder.

Files
- `docs/configs/appsettings.example.json` — comprehensive example for local/dev running. Use this to learn the available settings and their structure.
- `docs/configs/appsettings.Docker.json` — example tuned for running inside Docker (paths reference `/app/blocklists` for mounted blocklists and uses an alternate DNS port 1053 to avoid requiring root on the host).

How to use
1. Copy the example you want to use to the expected config location or mount it into the container:
   - Local run: `cp docs/configs/appsettings.example.json src/DnsForwarder/appsettings.Development.json`
   - Docker run: mount `docs/configs/appsettings.Docker.json` to `/app/appsettings.Docker.json` (the provided docker-compose does this by default).

2. Adjust the `BlocklistSources.Files` or `BlocklistSources.Urls` entries to point to your blocklist files/URLs.

3. If you enable DHCP or NTP in `Service`, be aware these features are experimental. DHCP requires listening on UDP port 67 and may conflict with existing DHCP servers on your network — run in an isolated lab environment.

Notes on important fields
- Service.DnsPort / AlternateDnsPort: The UDP port the service will bind to for DNS requests. On Linux/macOS binding to 53 typically requires root privileges.
- Resolvers / DefaultResolvers: Upstream resolver definitions. `Rule` is optional and may be a regex or wildcard-treated rule depending on your configuration.
- Caching: Controls in-memory DNS caching. `TryGetPooled` and `TryGet` semantics mean the cache uses pooled buffers; do not rely on the buffers outside the service boundaries.
- BlocklistSources.Files: Array of paths pointing to blocklist files. When running in Docker, mount a host directory to `/app/blocklists` and reference `/app/blocklists/<file>` here.
- Metrics: Port and path where the service exposes Prometheus metrics.

Example (quick start)
- Run with docker-compose from repository root (the compose mounts `docs/configs/appsettings.Docker.json`):

  docker-compose -f docs/docker-compose.yml up --build

- Verify metrics are exposed at `http://localhost:1080/metrics` and that Prometheus (if enabled in compose) discovers the `dnsforwarder` target.

If you want me to add more configuration variants (Kubernetes ConfigMap examples, systemd service file snippets, or different sample blocklist formats), tell me which and I will add them to the branch.