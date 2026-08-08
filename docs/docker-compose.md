# Docker Compose

The Container image can be pulled from guidcruncher/dns-forwarder

```bash
docker pull docker.io/guidcruncher/dns-forwarder:latest
```

## Docker Run

```bash
docker run \
	-p 53:53/tcp \
	-p 53:53/udp \
	-p 67:67/udp \
	-p 123:123/udp \
	-v ./appsettings.json:/app/appsettings.json:ro \
	-v ./dnsforwarder:/var/lib/dnsforwarder \
	--cap-add NET_ADMIN \
	--cap-add SYS_TIME \
	--cap-add SYS_NICE \ 
	docker.io/guidcruncher/dns-forwarder:latest
```

## Docker Compose 

```yaml
services:
  dns-forwarder:
    image: guidcruncher/dns-forwarder:latest
    container_name: dns-forwarder
    hostname: dns-forwarder
    restart: unless-stopped
    # DNS uses UDP port 53
    ports:
      - "53:53/tcp"
      - "53:53/udp"
      - "67:67/udp"
      - "123:123/udp"
    volumes:
      - ./appsettings.json:/app/appsettings.json:ro
      - ./dnsforwarder:/var/lib/dnsforwarder
    cap_add:
      - NET_ADMIN
      - SYS_TIME
      - SYS_NICE
    # Optional: run with host networking for maximum performance
    # network_mode: host
```
