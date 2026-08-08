# DnsForwarder — Documentation

DnsForwarder is a lightweight, high-performance DNS forwarder written in .NET. This docs folder contains both user-facing usage guides and developer-facing technical documentation.

Contents
- README.md — this index and quick start
- usage.md — examples for running and configuring the service
- technical.md — architecture, component descriptions, threading and caching behaviour
- examples.md — ready-to-run configuration and CLI examples
- docker-compose.yml — example docker-compose to run DnsForwarder and supporting services
- diagrams.md — architecture and flow diagrams (mermaid)
- diagrams/README.md — static SVG exports index and viewing instructions
- configs/ — example configuration files (appsettings.example.json, appsettings.Docker.json)
- prometheus.yml — minimal Prometheus config (scrape dnsforwarder:1080)

Quick links
- Diagrams (static SVGs): diagrams/README.md
- Configuration examples: configurations.md
- Docker Compose example: docker-compose.yml

If you'd like a published docs site (MkDocs or similar) or a PR opened against `dev`, tell me and I will prepare it.