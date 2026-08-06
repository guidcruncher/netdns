using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

using Microsoft.Extensions.Logging;

namespace DnsForwarder.Dhcp;

public sealed class DhcpServerEngine
{
    private readonly ILogger<DhcpServerEngine> _logger;
    private readonly DhcpOptions _config;
    private readonly IDhcpLeaseStore _store;
    private readonly IUdpTransport _transport;

    private readonly DhcpLeaseEngine _leaseEngine;
    private readonly CidrPoolAllocator _pool;
    private readonly ArpConflictDetector _arp;

    private readonly IPAddress _serverId;
    private readonly IPAddress _router;
    private readonly IPAddress _dns;

    // -------------------------------
    // Test-mode support
    // -------------------------------
    private readonly bool _testMode;
    private IPEndPoint? _lastClient;

    public DhcpServerEngine(
        ILogger<DhcpServerEngine> logger,
        DhcpOptions config,
        IDhcpLeaseStore store,
        IUdpTransport transport,
        bool testMode = false)
    {
        _logger = logger;
        _config = config;
        _store = store;
        _transport = transport;

        _testMode = testMode;

        _pool = new CidrPoolAllocator(config.PoolCidr);
        _leaseEngine = new DhcpLeaseEngine(store, _pool);
        _arp = new ArpConflictDetector(IPAddress.Parse(config.ListenAddress));

        _serverId = IPAddress.Parse(config.ServerIdentifier);
        _router = IPAddress.Parse(config.Router);
        _dns = IPAddress.Parse(config.DnsServer);
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
                result = await _transport.ReceiveAsync(ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            // Track client endpoint for unicast test mode
            _lastClient = result.RemoteEndPoint;

            var req = DhcpPacketCodec.Parse(result.Buffer);

            var type = req.GetMessageType();
            var mac = new PhysicalAddress(req.Chaddr.Take(req.Hlen).ToArray());

            switch (type)
            {
                case DhcpMessageType.Discover:
                    await HandleDiscoverAsync(req, mac);
                    break;

                case DhcpMessageType.Request:
                    await HandleRequestAsync(req, mac);
                    break;

                case DhcpMessageType.Release:
                    HandleRelease(mac);
                    break;

                case DhcpMessageType.Decline:
                    HandleDecline(req, mac);
                    break;

                case DhcpMessageType.Inform:
                    await HandleInformAsync(req);
                    break;

                default:
                    _logger.LogWarning("Unhandled DHCP message type: {Type}", type);
                    break;
            }
        }
    }

    // -------------------------------
    // Helper: choose reply endpoint
    // -------------------------------
    private IPEndPoint ReplyEndpoint()
    {
        if (_testMode && _lastClient != null)
            return _lastClient;

        return new IPEndPoint(IPAddress.Broadcast, 68);
    }

    // ------------------------------------------------------------
    // DISCOVER → OFFER
    // ------------------------------------------------------------
    private async Task HandleDiscoverAsync(DhcpPacket req, PhysicalAddress mac)
    {
        _logger.LogInformation("DHCP DISCOVER from {Mac}", mac);

        var lease = await _leaseEngine.AllocateWithArpCheck(mac, TimeSpan.FromHours(1), _arp);

        var offer = DhcpPacketCodec.BuildOffer(
            req,
            lease.Ip,
            _serverId,
            _router,
            _dns,
            TimeSpan.FromHours(1));

        await _transport.SendAsync(offer, offer.Length, ReplyEndpoint());

        _logger.LogInformation("Sent OFFER {Ip} to {Mac}", lease.Ip, mac);
    }

    // ------------------------------------------------------------
    // REQUEST → ACK or NAK
    // ------------------------------------------------------------
    private async Task HandleRequestAsync(DhcpPacket req, PhysicalAddress mac)
    {
        var requestedIp = req.GetRequestedIp();
        var serverIdOpt = req.GetServerIdentifier();

        _logger.LogInformation("DHCP REQUEST from {Mac} for {Ip}", mac, requestedIp);

        // Wrong server → NAK
        if (serverIdOpt != null && !serverIdOpt.Equals(_serverId))
        {
            var nak = DhcpPacketCodec.BuildNak(req, _serverId);
            await _transport.SendAsync(nak, nak.Length, ReplyEndpoint());
            _logger.LogWarning("Sent NAK to {Mac} (wrong server)", mac);
            return;
        }

        var lease = await _leaseEngine.AllocateWithArpCheck(mac, TimeSpan.FromHours(1), _arp);

        if (requestedIp != null && !requestedIp.Equals(lease.Ip))
        {
            var nak = DhcpPacketCodec.BuildNak(req, _serverId);
            await _transport.SendAsync(nak, nak.Length, ReplyEndpoint());
            _logger.LogWarning("Sent NAK to {Mac} (requested {ReqIp}, assigned {LeaseIp})",
                mac, requestedIp, lease.Ip);
            return;
        }

        var ack = DhcpPacketCodec.BuildAck(
            req,
            lease.Ip,
            _serverId,
            _router,
            _dns,
            TimeSpan.FromHours(1));

        await _transport.SendAsync(ack, ack.Length, ReplyEndpoint());

        _logger.LogInformation("Sent ACK {Ip} to {Mac}", lease.Ip, mac);
    }

    // ------------------------------------------------------------
    // RELEASE → remove lease
    // ------------------------------------------------------------
    private void HandleRelease(PhysicalAddress mac)
    {
        _logger.LogInformation("DHCP RELEASE from {Mac}", mac);
        _leaseEngine.Release(mac);
    }

    // ------------------------------------------------------------
    // DECLINE → mark IP bad + remove lease
    // ------------------------------------------------------------
    private void HandleDecline(DhcpPacket req, PhysicalAddress mac)
    {
        var requestedIp = req.GetRequestedIp();
        _logger.LogWarning("DHCP DECLINE from {Mac} for {Ip}", mac, requestedIp);

        _leaseEngine.Release(mac);

        if (requestedIp != null)
            _leaseEngine.Decline(requestedIp);
    }

    // ------------------------------------------------------------
    // INFORM → ACK with config only
    // ------------------------------------------------------------
    private async Task HandleInformAsync(DhcpPacket req)
    {
        _logger.LogInformation("DHCP INFORM from client with IP {Ip}", req.Ciaddr);

        var ack = DhcpPacketCodec.BuildInformAck(
            req,
            _serverId,
            _router,
            _dns);

        await _transport.SendAsync(ack, ack.Length, ReplyEndpoint());

        _logger.LogInformation("Sent INFORM-ACK to client {Ip}", req.Ciaddr);
    }
}
