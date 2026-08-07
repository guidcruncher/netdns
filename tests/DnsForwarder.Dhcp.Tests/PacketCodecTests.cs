using System.Net;

using DnsForwarder.Dhcp;

using Xunit;

namespace DnsForwarder.Dhcp.Tests;

public class PacketCodecTests
{
    [Fact]
    public void BuildOffer_ShouldSetCorrectMessageType()
    {
        var req = PacketFactory.Discover();
        var offer = DhcpPacketCodec.BuildOffer(
            req,
            IPAddress.Parse("192.168.10.50"),
            IPAddress.Parse("192.168.10.1"),
            IPAddress.Parse("192.168.10.1"),
            IPAddress.Parse("1.1.1.1"),
        null,
            TimeSpan.FromHours(1));

        var parsed = DhcpPacketCodec.Parse(offer);
        Assert.Equal(DhcpMessageType.Offer, parsed.GetMessageType());
    }

    [Fact]
    public void BuildAck_ShouldSetYiaddr()
    {
        var req = PacketFactory.Request();
        var ack = DhcpPacketCodec.BuildAck(
            req,
            IPAddress.Parse("192.168.10.55"),
            IPAddress.Parse("192.168.10.1"),
            IPAddress.Parse("192.168.10.1"),
            IPAddress.Parse("1.1.1.1"),
        null,
            TimeSpan.FromHours(1));

        var parsed = DhcpPacketCodec.Parse(ack);
        Assert.Equal("192.168.10.55", parsed.Yiaddr.ToString());
    }

    [Fact]
    public void BuildNak_ShouldSetMessageTypeNak()
    {
        var req = PacketFactory.Request();
        var nak = DhcpPacketCodec.BuildNak(req, IPAddress.Parse("192.168.10.1"));

        var parsed = DhcpPacketCodec.Parse(nak);
        parsed.GetMessageType().Should().Be(DhcpMessageType.Nak);
    }
}
