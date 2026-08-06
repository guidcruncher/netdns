using System.Net;
using DnsForwarder.Dhcp;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DnsForwarder.Dhcp.Tests;

public class ServerEngineTests
{
[Fact]
public async Task Discover_ShouldProduceOffer()
{
    var store = new InMemoryDhcpLeaseStore();
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

    var logger = NullLogger<DhcpServerEngine>.Instance;
    var fakeUdp = new FakeUdpClient();
    var engine = new DhcpServerEngine(logger, opts, store, fakeUdp);

    var discoverBytes = PacketFactory.DiscoverBytes();
    await fakeUdp.InjectReceive(discoverBytes);

    fakeUdp.CancelAfter(50);

    try
    {
        await engine.RunAsync(fakeUdp.CancellationToken);
    }
    catch (OperationCanceledException)
    {
        // expected
    }

    fakeUdp.SentPackets.Should().NotBeEmpty();

    var parsed = DhcpPacketCodec.Parse(fakeUdp.SentPackets.First());
    parsed.GetMessageType().Should().Be(DhcpMessageType.Offer);
}
}
