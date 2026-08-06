using System.Net;
using System.Net.Sockets;

using DnsForwarder.Dhcp;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

namespace DnsForwarder.Dhcp.Tests.Integration;

public class DiscoverOfferTests
{
    [Fact]
    public async Task Discover_ShouldProduceRealOffer()
    {
        var opts = new DhcpOptions
        {
            Enabled = true,
            ListenAddress = "127.0.0.1",
            ListenPort = 6767,
            PoolCidr = "192.168.10.0/29",
            ServerIdentifier = "192.168.10.1",
            Router = "192.168.10.1",
            DnsServer = "1.1.1.1"
        };

        var store = new InMemoryDhcpLeaseStore();
        var transport = new UdpTransport(IPAddress.Parse(opts.ListenAddress), opts.ListenPort);
        var engine = new DhcpServerEngine(NullLogger<DhcpServerEngine>.Instance, opts, store, transport);

        var cts = new CancellationTokenSource();
        _ = engine.RunAsync(cts.Token);

        var client = new RealDhcpClient(6768);

        var discover = PacketFactory.DiscoverBytes();
        await client.SendAsync(discover);

        var result = await client.ReceiveAsync(cts.Token);

        var parsed = DhcpPacketCodec.Parse(result.Buffer);
        parsed.GetMessageType().Should().Be(DhcpMessageType.Offer);

        cts.Cancel();
    }

    [Fact]
    public async Task Request_ShouldProduceAck()
    {
        var opts = new DhcpOptions
        {
            Enabled = true,
            ListenAddress = "127.0.0.1",
            ListenPort = 6767,
            PoolCidr = "192.168.10.0/29",
            ServerIdentifier = "192.168.10.1",
            Router = "192.168.10.1",
            DnsServer = "1.1.1.1"
        };

        var store = new InMemoryDhcpLeaseStore();
        var transport = new UdpTransport(IPAddress.Parse(opts.ListenAddress), opts.ListenPort);
        var engine = new DhcpServerEngine(NullLogger<DhcpServerEngine>.Instance, opts, store, transport);

        var cts = new CancellationTokenSource();
        _ = engine.RunAsync(cts.Token);

        var client = new RealDhcpClient(6768);

        // First send DISCOVER
        var discover = PacketFactory.DiscoverBytes();
        await client.SendAsync(discover);
        var offer = await client.ReceiveAsync(cts.Token);
        var parsedOffer = DhcpPacketCodec.Parse(offer.Buffer);

        // Now send REQUEST for the offered IP
        var request = PacketFactory.RequestBytes(parsedOffer);
        await client.SendAsync(request);

        var ack = await client.ReceiveAsync(cts.Token);
        var parsedAck = DhcpPacketCodec.Parse(ack.Buffer);

        parsedAck.GetMessageType().Should().Be(DhcpMessageType.Ack);

        cts.Cancel();
    }

    [Fact]
    public async Task Inform_ShouldProduceInformAck()
    {
        var opts = new DhcpOptions
        {
            Enabled = true,
            ListenAddress = "127.0.0.1",
            ListenPort = 6767,
            PoolCidr = "192.168.10.0/29",
            ServerIdentifier = "192.168.10.1",
            Router = "192.168.10.1",
            DnsServer = "1.1.1.1"
        };

        var store = new InMemoryDhcpLeaseStore();
        var transport = new UdpTransport(IPAddress.Parse(opts.ListenAddress), opts.ListenPort);
        var engine = new DhcpServerEngine(NullLogger<DhcpServerEngine>.Instance, opts, store, transport);

        var cts = new CancellationTokenSource();
        _ = engine.RunAsync(cts.Token);

        var client = new RealDhcpClient(6768);

        var inform = PacketFactory.InformBytes(IPAddress.Parse("192.168.10.50"));
        await client.SendAsync(inform);

        var ack = await client.ReceiveAsync(cts.Token);
        var parsedAck = DhcpPacketCodec.Parse(ack.Buffer);

        parsedAck.GetMessageType().Should().Be(DhcpMessageType.Ack);

        cts.Cancel();
    }


}
