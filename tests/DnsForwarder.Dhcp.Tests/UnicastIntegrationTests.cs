using System.Net;
using System.Net.Sockets;
using DnsForwarder.Dhcp;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DnsForwarder.Dhcp.Tests;

public class UnicastIntegrationTests
{
    [Fact]
    public async Task Discover_ShouldProduce_UnicastOffer()
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
        var transport = new UdpTransport(IPAddress.Loopback, 6767);

        // Enable unicast test mode
        var engine = new DhcpServerEngine(
            NullLogger<DhcpServerEngine>.Instance,
            opts,
            store,
            transport,
            testMode: true);

        var cts = new CancellationTokenSource();
        _ = engine.RunAsync(cts.Token);

        // Client listens on 6868
        var client = new RealDhcpClient(6868);

        // Send DISCOVER
        await client.SendAsync(PacketFactory.DiscoverBytes());

        // Receive OFFER
        var offer = await client.ReceiveAsync(cts.Token);

        // Assert unicast: server replied directly to 6868
        offer.RemoteEndPoint.Port.Should().Be(6767);
        offer.RemoteEndPoint.Address.Should().Be(IPAddress.Loopback);

        var parsed = DhcpPacketCodec.Parse(offer.Buffer);
        parsed.GetMessageType().Should().Be(DhcpMessageType.Offer);

        cts.Cancel();
    }

[Fact]
public async Task Request_ShouldProduce_UnicastAck()
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
    var transport = new UdpTransport(IPAddress.Loopback, 6767);

    var engine = new DhcpServerEngine(
        NullLogger<DhcpServerEngine>.Instance,
        opts,
        store,
        transport,
        testMode: true);

    var cts = new CancellationTokenSource();
    _ = engine.RunAsync(cts.Token);

    var client = new RealDhcpClient(6868);

    // DISCOVER → OFFER
    await client.SendAsync(PacketFactory.DiscoverBytes());
    var offer = await client.ReceiveAsync(cts.Token);
    var parsedOffer = DhcpPacketCodec.Parse(offer.Buffer);

    // REQUEST → ACK
    await client.SendAsync(PacketFactory.RequestBytes(parsedOffer));
    var ack = await client.ReceiveAsync(cts.Token);

    // Assert unicast
    ack.RemoteEndPoint.Port.Should().Be(6767);
    ack.RemoteEndPoint.Address.Should().Be(IPAddress.Loopback);

    var parsedAck = DhcpPacketCodec.Parse(ack.Buffer);
    parsedAck.GetMessageType().Should().Be(DhcpMessageType.Ack);

    cts.Cancel();
}

[Fact]
public async Task Inform_ShouldProduce_UnicastInformAck()
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
    var transport = new UdpTransport(IPAddress.Loopback, 6767);

    var engine = new DhcpServerEngine(
        NullLogger<DhcpServerEngine>.Instance,
        opts,
        store,
        transport,
        testMode: true);

    var cts = new CancellationTokenSource();
    _ = engine.RunAsync(cts.Token);

    var client = new RealDhcpClient(6868);

    // INFORM
    await client.SendAsync(PacketFactory.InformBytes(IPAddress.Parse("192.168.10.50")));

    var ack = await client.ReceiveAsync(cts.Token);

    // Assert unicast
    ack.RemoteEndPoint.Port.Should().Be(6767);
    ack.RemoteEndPoint.Address.Should().Be(IPAddress.Loopback);

    var parsedAck = DhcpPacketCodec.Parse(ack.Buffer);
    parsedAck.GetMessageType().Should().Be(DhcpMessageType.Ack);

    cts.Cancel();
}




}
