using System.Net;

using DnsForwarder.Dhcp;

using FluentAssertions;

using Xunit;

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
            TimeSpan.FromHours(1));

        var parsed = DhcpPacketCodec.Parse(offer);
        parsed.GetMessageType().Should().Be(DhcpMessageType.Offer);
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
            TimeSpan.FromHours(1));

        var parsed = DhcpPacketCodec.Parse(ack);
        parsed.Yiaddr.ToString().Should().Be("192.168.10.55");
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
