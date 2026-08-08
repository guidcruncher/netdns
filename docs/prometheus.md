# Prometheus Configuration

```yaml
global:
  scrape_interval: 15s
  evaluation_interval: 15s

scrape_configs:
  - job_name: 'dnsforwarder'
    metrics_path: '/metrics'
    static_configs:
      - targets: ['dnsforwarder:1080']
        labels:
          service: dnsforwarder
```
