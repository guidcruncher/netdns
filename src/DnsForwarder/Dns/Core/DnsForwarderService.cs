using System.Net;

using Microsoft.Extensions.Logging;

namespace DnsForwarder.Dns.Core;

public sealed class DnsForwarderService
{
    private readonly ILogger<DnsForwarderService> _logger;
    private readonly DnsForwarderOptions _options;
    private readonly IDnsClient _defaultClient;
    private readonly RuleEngine.RuleEngine _ruleEngine;

    public DnsForwarderService(
        ILogger<DnsForwarderService> logger,
        DnsForwarderOptions options,
        IDnsClient defaultClient,
        RuleEngine.RuleEngine ruleEngine)
    {
        _logger = logger;
        _options = options;
        _defaultClient = defaultClient;
        _ruleEngine = ruleEngine;
    }

    public async Task<byte[]?> ProcessAsync(
        byte[] request,
        IPEndPoint remote,
        CancellationToken ct)
    {
        var requestId = Guid.NewGuid().ToString("N");

        _logger.LogInformation(
            "Request {RequestId}: Received DNS request from {Remote} ({Length} bytes)",
            requestId,
            remote,
            request.Length);

        var message = DnsParser.Parse(request);

        var q = message.Questions.FirstOrDefault();
        if (q is null)
        {
            _logger.LogWarning(
                "Request {RequestId}: Received DNS message with no questions from {Remote}",
                requestId,
                remote);
            return null;
        }

        _logger.LogInformation(
            "Request {RequestId}: DNS query from {Remote} for {Domain} ({Type})",
            requestId,
            remote,
            q.Name,
            q.Type);

        var ruleResult = _ruleEngine.Match(q.Name, requestId);

        // --- BLOCK RULE ---
        if (ruleResult.Block)
        {
            _logger.LogInformation(
                "Request {RequestId}: Blocking query for {Domain} from {Remote}",
                requestId,
                q.Name,
                remote);

            var blocked = DnsParser.BuildBlockedResponse(message);

            // PATCH DNS ID
            blocked[0] = request[0];
            blocked[1] = request[1];

            return blocked;
        }

        var active = ruleResult.Upstreams[0];

        // --- CACHE CHECK ---
        if (_ruleEngine.Cache.TryGet(q.Name, out var cachedResponse) && cachedResponse is not null)
        {
            _logger.LogInformation(
                "Request {RequestId}: Cache HIT for {Domain} (served without forwarding)",
                requestId,
                q.Name);

            // PATCH DNS ID
            cachedResponse[0] = request[0];
            cachedResponse[1] = request[1];

            return cachedResponse;
        }

        _logger.LogInformation(
            "Request {RequestId}: Cache MISS for {Domain} (forwarding to upstream {Upstream})",
            requestId,
            q.Name,
            active.Name);

        _logger.LogInformation(
            "Request {RequestId}: Forwarding {Domain} ({Type}) from {Remote} to upstream {Upstream}",
            requestId,
            q.Name,
            q.Type,
            remote,
            active.Name);

        // --- FORWARD + FALLBACK ---
        var response = await _ruleEngine.QueryAsync(q.Name, request, requestId, ct);

        _logger.LogInformation(
            "Request {RequestId}: Completed DNS query for {Domain} from {Remote} using upstream {Upstream}",
            requestId,
            q.Name,
            remote,
            active.Name);

        response[0] = request[0];
        response[1] = request[1];

        return response;
    }
}
