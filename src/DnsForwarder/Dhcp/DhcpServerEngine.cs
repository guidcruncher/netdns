using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using DnsForwarder.Events;

using Microsoft.Extensions.Logging;

namespace DnsForwarder.Dhcp;

public sealed class DhcpServerEngine
{
    private readonly ILogger<DhcpServerEngine> _logger;
    private readonly DhcpOptions _config;
    private readonly IDhcpLeaseStore _store;
    private readonly IUdpTransport _transport;
    private readonly IDhcpMetrics _metrics;

    private readonly DhcpLeaseEngine _leaseEngine;
    private readonly CidrPoolAllocator _pool;
    private readonly ArpConflictDetector _arp;

    private readonly IPAddress _serverId;
    private readonly IPAddress _router;
    private readonly IPAddress _dns;
    private readonly IPAddress? _ntp;

    private readonly bool _testMode;
    private IPEndPoint? _lastClient;

    public DhcpServerEngine(
        ILogger<DhcpServerEngine> logger,
        DhcpOptions config,
        IDhcpLeaseStore store,
        IUdpTransport transport,
        IDhcpMetrics metrics,
        bool testMode = false)
    {
        _logger = logger;
        _config = config;
        _store = store;
        _transport = transport;
        _metrics = metrics;

        _testMode = testMode;

        _pool = new CidrPoolAllocator(config.PoolCidr);
        _leaseEngine = new DhcpLeaseEngine(store, _pool);
        _arp = new ArpConflictDetector(IPAddress.Parse(config.ListenAddress));

        _serverId = IPAddress.Parse(config.ServerIdentifier);
        _router = IPAddress.Parse(config.Router);
        _dns = IPAddress.Parse(config.DnsServer);

        if (!string.IsNullOrWhiteSpace(config.NtpServer))
            _ntp = IPAddress.Parse(config.NtpServer);
        else
            _ntp = null;
    }

    private void LogClientName(DhcpPacket req, PhysicalAddress mac)
    {
        var hostOpt = req.Options.FirstOrDefault(o => o.Code == 12);
        var fqdnOpt = req.Options.FirstOrDefault(o => o.Code == 81);

        string? host = hostOpt != null
            ? System.Text.Encoding.ASCII.GetString(hostOpt.Data)
            : null;

        string? fqdn = fqdnOpt != null
            ? System.Text.Encoding.ASCII.GetString(fqdnOpt.Data)
            : null;

        if (!string.IsNullOrEmpty(fqdn))
        {
            _logger.LogInformation("Client {Mac} FQDN: {Fqdn}", mac, fqdn);
        }
        else if (!string.IsNullOrEmpty(host))
        {
            _logger.LogInformation("Client {Mac} hostname: {Host}", mac, host);
        }
        else
        {
            _logger.LogInformation("Client {Mac} did not send hostname", mac);
        }
    }

    public async Task RunAsync(CancellationToken ct)
    {
        _logger.LogInformation("DHCP server listening on {Address}:{Port}",
            _config.ListenAddress, _config.ListenPort);

        while (!ct.IsCancellationRequested)
        {
            UdpReceiveResult result;

            try
            {
                result = await _transport.ReceiveAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            _lastClient = result.RemoteEndPoint;

            var req = DhcpPacketCodec.Parse(result.Buffer);
            var type = req.GetMessageType();
            var mac = new PhysicalAddress(req.Chaddr.Take(req.Hlen).ToArray());

            switch (type)
            {
                case DhcpMessageType.Discover:
                    LogClientName(req, mac);
                    await HandleDiscoverAsync(req, mac).ConfigureAwait(false);
                    break;

                case DhcpMessageType.Request:
                    LogClientName(req, mac);
                    await HandleRequestAsync(req, mac).ConfigureAwait(false);
                    break;

                case DhcpMessageType.Release:
                    HandleRelease(req, mac);
                    break;

                case DhcpMessageType.Decline:
                    HandleDecline(req, mac);
                    break;

                case DhcpMessageType.Inform:
                    LogClientName(req, mac);
                    await HandleInformAsync(req).ConfigureAwait(false);
                    break;

                default:
                    _logger.LogWarning("Unhandled DHCP message type: {Type}", type);
                    break;
            }
        }
    }

    private IPEndPoint ReplyEndpoint()
    {
        if (_testMode && _lastClient is not null)
            return _lastClient;

        return new IPEndPoint(IPAddress.Broadcast, 68);
    }

    private async Task HandleDiscoverAsync(DhcpPacket req, PhysicalAddress mac)
    {
        _logger.LogInformation("DHCP DISCOVER from {Mac}", mac);

        var lease = await _leaseEngine.AllocateWithArpCheck(
            mac,
            TimeSpan.FromHours(1),
            _arp).ConfigureAwait(false);

        var offer = DhcpPacketCodec.BuildOffer(
            req,
            lease.Ip,
            _serverId,
            _router,
            _dns,
            _ntp,
            TimeSpan.FromHours(1));

        await _transport.SendAsync(offer, offer.Length, ReplyEndpoint())
                        .ConfigureAwait(false);

        _logger.LogInformation("Sent OFFER {Ip} to {Mac}", lease.Ip, mac);
    }

    private async Task HandleRequestAsync(DhcpPacket req, PhysicalAddress mac)
    {
        var requestedIp = req.GetRequestedIp();
        var serverIdOpt = req.GetServerIdentifier();

        _logger.LogInformation("DHCP REQUEST from {Mac} for {Ip}", mac, requestedIp);

        if (serverIdOpt is not null && !serverIdOpt.Equals(_serverId))
        {
            var nak = DhcpPacketCodec.BuildNak(req, _serverId);
            await _transport.SendAsync(nak, nak.Length, ReplyEndpoint())
                            .ConfigureAwait(false);

            _metrics.NakSent(new DhcpNakEvent(
                Timestamp: DateTime.UtcNow,
                Mac: mac,
                RequestedIp: requestedIp,
                Reason: "Wrong server identifier"));

            _logger.LogWarning("Sent NAK to {Mac} (wrong server)", mac);
            return;
        }

        var lease = await _leaseEngine.AllocateWithArpCheck(
            mac,
            TimeSpan.FromHours(1),
            _arp).ConfigureAwait(false);

        if (requestedIp is not null && !requestedIp.Equals(lease.Ip))
        {
            var nak = DhcpPacketCodec.BuildNak(req, _serverId);
            await _transport.SendAsync(nak, nak.Length, ReplyEndpoint())
                            .ConfigureAwait(false);

            _metrics.NakSent(new DhcpNakEvent(
                Timestamp: DateTime.UtcNow,
                Mac: mac,
                RequestedIp: requestedIp,
                Reason: "Requested IP mismatch"));

            _logger.LogWarning(
                "Sent NAK to {Mac} (requested {ReqIp}, assigned {LeaseIp})",
                mac, requestedIp, lease.Ip);
            return;
        }

        var ack = DhcpPacketCodec.BuildAck(
            req,
            lease.Ip,
            _serverId,
            _router,
            _dns,
            _ntp,
            TimeSpan.FromHours(1));

        await _transport.SendAsync(ack, ack.Length, ReplyEndpoint())
                        .ConfigureAwait(false);

        _metrics.LeaseAllocated(new DhcpLeaseAllocatedEvent(
            Timestamp: DateTime.UtcNow,
            ClientIp: lease.Ip,
            Mac: mac,
            ClientName: req.GetHostName() ?? req.GetFqdn(),
            ServerId: _serverId,
            LeaseStart: DateTime.UtcNow,
            LeaseExpiry: DateTime.UtcNow.AddHours(1)));

        _logger.LogInformation("Sent ACK {Ip} to {Mac}", lease.Ip, mac);
    }

    private void HandleRelease(DhcpPacket req, PhysicalAddress mac)
    {
        _logger.LogInformation("DHCP RELEASE from {Mac}", mac);
        _leaseEngine.Release(mac);

        _metrics.LeaseReleased(new DhcpLeaseReleasedEvent(
            Timestamp: DateTime.UtcNow,
            Mac: mac,
            ClientIp: req.Ciaddr,
            ClientName: req.GetHostName() ?? req.GetFqdn()));
    }

    private void HandleDecline(DhcpPacket req, PhysicalAddress mac)
    {
        var requestedIp = req.GetRequestedIp();
        _logger.LogWarning("DHCP DECLINE from {Mac} for {Ip}", mac, requestedIp);

        _leaseEngine.Release(mac);

        if (requestedIp is not null)
            _leaseEngine.Decline(requestedIp);

        _metrics.NakSent(new DhcpNakEvent(
            Timestamp: DateTime.UtcNow,
            Mac: mac,
            RequestedIp: requestedIp,
            Reason: "Client declined assigned IP"));
    }

    private async Task HandleInformAsync(DhcpPacket req)
    {
        _logger.LogInformation("DHCP INFORM from client with IP {Ip}", req.Ciaddr);

        var ack = DhcpPacketCodec.BuildInformAck(
            req,
            _serverId,
            _router,
            _dns,
            _ntp);

        await _transport.SendAsync(ack, ack.Length, ReplyEndpoint())
                        .ConfigureAwait(false);

        _logger.LogInformation("Sent INFORM-ACK to client {Ip}", req.Ciaddr);
    }
}

