# DnsForwarder

![DnsForwarder logo](assets/logo.svg)

DnsForwarder is a lightweight, high-performance DNS forwarder implemented in .NET. This documentation site contains usage guides, technical details, configuration examples, and architecture diagrams.

Quick links
- Usage guide — usage.md
- Technical overview — technical.md
- Configuration examples — configurations.md
- Examples & runbooks — examples.md
- Docker Compose example — docker-compose.yml
- Prometheus config — prometheus.yml
- Diagrams (static SVGs) — diagrams/README.md

Getting started (quick)
1. Review `docs/configs/appsettings.Docker.json` for an example Docker config.
2. Start locally:
   - dotnet run --project src/DnsForwarder/DnsForwarder.csproj -- --config src/DnsForwarder/appsettings.Development.json
3. Or use docker-compose:
   - docker-compose -f docs/docker-compose.yml up --build

