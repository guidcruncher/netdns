using System.Net;
using System.Text.RegularExpressions;

using DnsForwarder.Dns.Core;
using DnsForwarder.Dns.Filtering;
using DnsForwarder.Utils;

using Microsoft.Extensions.Logging;

namespace DnsForwarder.Dns.RuleEngine;

internal sealed class QueryExecutor
{
    private readonly DnsCache _cache;
    private readonly ILogger _logger;

    public QueryExecutor(DnsCache cache, ILogger logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<byte[]?> ExecuteAsync(
        List<UpstreamEntry> upstreams,
        string domain,
        byte[] request,
        string requestId,
        CancellationToken ct)
    {
        foreach (var upstream in upstreams)
        {
            try
            {
                _logger.LogDebug("Request {RequestId}: Querying upstream {Upstream} for {Domain}",
                    requestId, upstream.Name, domain);

                var resp = await upstream.Client.QueryAsync(request, ct);

                if (resp.Length < 4)
                    continue;

                var rcode = resp[3] & 0x0F;

                if (rcode == 2)
                {
                    _logger.LogWarning("Request {RequestId}: Upstream {Upstream} returned SERVFAIL for {Domain}",
                        requestId, upstream.Name, domain);
                    continue;
                }

                var copy = resp.ToArray();
                int ttl = TtlExtractor.ExtractTtl(copy);

                if (ttl > 0)
                {
                    _logger.LogInformation("Request {RequestId}: TTL for {Domain} is {TTL}s (via {Upstream})",
                        requestId, domain, ttl, upstream.Name);

                    _cache.Store(domain, copy, TimeSpan.FromSeconds(ttl));
                }
                else
                {
                    _logger.LogWarning("Request {RequestId}: No TTL found for {Domain} (via {Upstream})",
                        requestId, domain, upstream.Name);
                }

                return copy;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Request {RequestId}: Error querying upstream {Upstream} for {Domain}",
                    requestId, upstream.Name, domain);
            }
        }

        return null;
    }
}

