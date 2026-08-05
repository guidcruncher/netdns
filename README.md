# DnsForwarder

DnsForwarder is a lightweight, high‑performance DNS forwarder written in .NET. It supports rule‑based upstream selection, blocklists, allowlists, host overrides, caching, and wildcard matching.

## Features

*   Fast UDP DNS forwarding
*   Rule‑based upstream resolver selection
*   Blocklists and allowlists (regex, prefix, suffix, substring)
*   Host overrides with wildcard support
*   Longest‑core‑wins wildcard host specificity
*   DNS caching with TTL extraction
*   Static DNS responses for host overrides
*   Structured logging with request IDs
*   Modular rule engine and host matcher

## Architecture Overview

DnsForwarder is built around two core components:

### 1\. RuleEngine

The `RuleEngine` evaluates DNS queries and determines which upstream resolver to use. It supports:

*   Exact matches
*   Prefix matches (`example.*`)
*   Suffix matches (`*.example.com`)
*   Substring wildcard matches (`*ads*`)
*   Regex rules
*   Fallback resolvers

Wildcard rule matching uses the project’s original non‑generic `AhoCorasickMatcher`. Prefix and suffix rules use `PrefixTrie` and `SuffixTrie`.

### 2\. HostMatcher

Host overrides are handled by a dedicated `HostMatcher` class. It supports:

*   Exact host matches
*   Prefix wildcard (`example.*`)
*   Suffix wildcard (`*.example.com`)
*   Substring wildcard (`*ads*`)

Wildcard host specificity follows a simple rule:

> **Longest core wins.** The “core” is the pattern with `*` removed. The host override with the longest core is selected.

This behaviour mirrors dnsmasq and Unbound wildcard host resolution.

## Configuration

### Default Resolver

```
{
  "DefaultResolver": {
    "Name": "default",
    "Address": "1.1.1.1",
    "Port": 53
  }
}
```

### Custom Resolvers

Resolvers may include rule patterns:

```
{
  "Resolvers": [
    {
      "Name": "cloudflare",
      "Address": "1.1.1.1",
      "Port": 53,
      "Rule": "*.example.com"
    },
    {
      "Name": "blocker",
      "Block": true,
      "Rule": "*ads*"
    }
  ]
}
```

### Hosts Files

Hosts files follow standard `/etc/hosts` formatting:

```

127.0.0.1 localhost
10.0.0.5 *.example.com
192.168.1.10 nas.local
```

## Wildcard Host Matching

### Suffix Wildcard

```
10.0.0.1 *.example.com
```

### Prefix Wildcard

```
10.0.0.2 example.*
```

### Substring Wildcard

```
10.0.0.3 *ads*
```

### Specificity Rule

Given:

```

10.0.0.10 *.example.com
10.0.0.20 *ample.com
10.0.0.30 *ple.com
```

Query: `foo.example.com`

All three match, but the longest core is `example.com` (length 11), so `10.0.0.10` wins.

## Caching

DnsForwarder extracts TTL from upstream responses and stores answers in `DnsCache`. TTL is capped by `CachingOptions.TtlSeconds`.

```
{
  "Caching": {
    "Enabled": true,
    "TtlSeconds": 300,
    "MaxEntries": 10000
  }
}
```

## Unit Tests

The project includes tests for:

*   Hosts file parsing
*   Wildcard host matching
*   Longest‑core‑wins behaviour
*   Blocklist precedence over hosts

Example wildcard test:

```

10.0.0.10 *.example.com
10.0.0.20 *ample.com
10.0.0.30 *ple.com
```

Querying `foo.example.com` selects `10.0.0.10`.

## Logging

DnsForwarder uses structured logging with request IDs:

```

Request 123: Hosts override matched for foo.example.com
Request 456: Blocking domain adsdomain.com due to rule inline
Request 789: TTL for example.net is 120s (via cloudflare)
```
