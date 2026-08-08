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


## Example: Run in Development

From repo root:

```bash
make dev
```

## Example: Run Unit Tests

From repo root:

```bash
make test
```

## Example: Query & verify NXDOMAIN blocked response

After configuring a rule that blocks `ads.example.com`:

```bash
dig @127.0.0.1 -p 1053 ads.example.com
```

The response will have RCODE=3 (NXDOMAIN). Unit tests include a BuildBlockedResponse test.

## Example: Benchmark run

```bash
make benchmark
```

## Example: Query DNS return valid response

```bash
make dig
```

## Example: View metrics

```bash
make metrics
```
