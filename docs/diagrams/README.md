# Diagrams Index

This directory contains static SVG exports of the repository's architecture and flow diagrams for offline browsing.

Files

- diagrams/component.svg — High-level component diagram showing Client, DnsForwarder, RuleEngine, DnsCache, Upstreams, Blocklist Sources and Metrics.
- diagrams/dns-sequence.svg — Simplified DNS request sequence (client → forwarder → match → cache/upstream → response).
- diagrams/dhcp-sequence.svg — Simplified DHCP sequence (DISCOVER → OFFER → REQUEST → ACK) and allocator interaction.
- diagrams/ntp-sequence.svg — Simplified NTP request/response flow.

How to view

- Open the SVG files directly in your browser (Chrome, Firefox, Safari) or in an SVG-capable editor.
- In VS Code, install the "SVG Viewer" or use the built-in Markdown preview to render image references.
- To embed a diagram in Markdown, reference the SVG path (relative to the Markdown file), for example:

  ![Component diagram](component.svg)

Notes

- PNG versions are not included in this branch to preserve vector fidelity. If you need PNG exports for compatibility with tools that do not render SVG, ask and I will add them.
- These files are intended for documentation and design review; if you want higher-resolution or alternate styles (colours, fonts), I can regenerate and commit updated SVGs.
