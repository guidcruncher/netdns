using System.Net;
using System.Text.RegularExpressions;

using DnsForwarder.Dns.Core;
using DnsForwarder.Dns.Filtering;
using DnsForwarder.Utils;

using Microsoft.Extensions.Logging;

namespace DnsForwarder.Dns.RuleEngine;

internal sealed class ResolverChainBuilder
{
    private readonly DnsForwarderOptions _options;
    private readonly IDnsClient _defaultClient;
    private readonly IReadOnlyList<UpstreamEntry> _fallback;
    private readonly ILogger _logger;

    public ResolverChainBuilder(
        DnsForwarderOptions options,
        IDnsClient defaultClient,
        IReadOnlyList<UpstreamEntry> fallback,
        ILogger logger)
    {
        _options = options;
        _defaultClient = defaultClient;
        _fallback = fallback;
        _logger = logger;
    }

    public List<UpstreamEntry> BuildChain(RuleResult match, string domain, string requestId)
    {
        if (match.Upstreams.Count > 0)
        {
            _logger.LogDebug("Request {RequestId}: Using rule-based upstreams for {Domain}", requestId, domain);
            return new List<UpstreamEntry>(match.Upstreams);
        }

        if (_fallback.Count > 0)
        {
            _logger.LogDebug("Request {RequestId}: Using fallback resolver chain for {Domain}", requestId, domain);
            return new List<UpstreamEntry>(_fallback);
        }

        var upstreams = new List<UpstreamEntry>();

        if (_options.DefaultResolvers != null && _options.DefaultResolvers.Count > 0)
        {
            _logger.LogDebug("Request {RequestId}: Using default resolver chain for {Domain}", requestId, domain);

            foreach (var def in _options.DefaultResolvers)
            {
                var endpoint = new IPEndPoint(IPAddress.Parse(def.Address), def.Port);
                upstreams.Add(new UpstreamEntry(def.Name, new UdpDnsClient(endpoint)));
            }

            return upstreams;
        }

        _logger.LogDebug("Request {RequestId}: Using single default resolver for {Domain}", requestId, domain);
        upstreams.Add(new UpstreamEntry("default", _defaultClient));
        return upstreams;
    }
}
