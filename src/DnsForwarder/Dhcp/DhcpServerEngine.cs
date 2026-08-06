using System.Net;
using Microsoft.Extensions.Logging;

namespace DnsForwarder.Dhcp;

public sealed class DhcpServerEngine
{
    private readonly ILogger<DhcpServerEngine> _logger;
    private readonly DhcpOptions _config;
    private readonly IDhcpLeaseStore _store;

    public DhcpServerEngine(ILogger<DhcpServerEngine> logger, DhcpOptions config, IDhcpLeaseStore store)
    {
        _logger = logger;
        _config = config;
        _store = store;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        using var udp = new UdpClient(new IPEndPoint(IPAddress.Parse(_config.ListenAddress), _config.ListenPort));

        while (!ct.IsCancellationRequested)
        {
            var result = await udp.ReceiveAsync(ct);
            var req = DhcpPacketCodec.Parse(result.Buffer);

            var type = req.GetMessageType();

            if (type == DhcpMessageType.Discover)
            {
                var offeredIp = IPAddress.Parse("192.168.10.50");
                var serverId = IPAddress.Parse("192.168.10.1");
                var router = IPAddress.Parse("192.168.10.1");
                var dns = IPAddress.Parse("1.1.1.1");

                var offer = DhcpPacketCodec.BuildOffer(req, offeredIp, serverId, router, dns, TimeSpan.FromHours(1));
                await udp.SendAsync(offer, offer.Length, new IPEndPoint(IPAddress.Broadcast, 68));
            }
            else if (type == DhcpMessageType.Request)
            {
                var assignedIp = req.GetRequestedIp() ?? IPAddress.Parse("192.168.10.50");
                var serverId = IPAddress.Parse("192.168.10.1");
                var router = IPAddress.Parse("192.168.10.1");
                var dns = IPAddress.Parse("1.1.1.1");

                var ack = DhcpPacketCodec.BuildAck(req, assignedIp, serverId, router, dns, TimeSpan.FromHours(1));
                await udp.SendAsync(ack, ack.Length, new IPEndPoint(IPAddress.Broadcast, 68));
            }
        }
    }
}
