using System;
using System.Linq;
using System.Net;

using Xunit;

namespace DnsForwarder.Dhcp.Tests;

public class DhcpNtpOptionTests
{
    private DhcpPacket MakeBasePacket()
    {
        return new DhcpPacket
        {
            Op = 1,
            Htype = 1,
            Hlen = 6,
            Hops = 0,
            Xid = 0x12345678,
            Secs = 0,
            Flags = 0,
            Ciaddr = IPAddress.Parse("0.0.0.0"),
            Yiaddr = IPAddress.Parse("0.0.0.0"),
            Siaddr = IPAddress.Parse("0.0.0.0"),
            Giaddr = IPAddress.Parse("0.0.0.0"),
            Chaddr = new byte[16]
        };
    }

    [Fact]
    public void Offer_Includes_Ntp_When_Configured()
    {
        var req = MakeBasePacket();
        var serverId = IPAddress.Parse("192.168.1.1");
        var router = IPAddress.Parse("192.168.1.1");
        var dns = IPAddress.Parse("192.168.1.53");
        var ntp = IPAddress.Parse("192.168.1.123");

        var offerBytes = DhcpPacketCodec.BuildOffer(
            req,
            IPAddress.Parse("192.168.1.50"),
            serverId,
            router,
            dns,
            ntp,
            TimeSpan.FromHours(1));

        var parsed = DhcpPacketCodec.Parse(offerBytes);

        Assert.Contains(parsed.Options, o => o.Code == 42);
        Assert.Equal(ntp.GetAddressBytes(), parsed.Options.First(o => o.Code == 42).Data);
    }

    [Fact]
    public void Offer_Does_Not_Include_Ntp_When_Not_Configured()
    {
        var req = MakeBasePacket();
        var serverId = IPAddress.Parse("192.168.1.1");
        var router = IPAddress.Parse("192.168.1.1");
        var dns = IPAddress.Parse("192.168.1.53");

        var offerBytes = DhcpPacketCodec.BuildOffer(
            req,
            IPAddress.Parse("192.168.1.50"),
            serverId,
            router,
            dns,
            null,                     // <-- no NTP
            TimeSpan.FromHours(1));

        var parsed = DhcpPacketCodec.Parse(offerBytes);

        Assert.DoesNotContain(parsed.Options, o => o.Code == 42);
    }

    [Fact]
    public void Ack_Includes_Ntp_When_Configured()
    {
        var req = MakeBasePacket();
        var serverId = IPAddress.Parse("192.168.1.1");
        var router = IPAddress.Parse("192.168.1.1");
        var dns = IPAddress.Parse("192.168.1.53");
        var ntp = IPAddress.Parse("192.168.1.123");

        var ackBytes = DhcpPacketCodec.BuildAck(
            req,
            IPAddress.Parse("192.168.1.50"),
            serverId,
            router,
            dns,
            ntp,
            TimeSpan.FromHours(1));

        var parsed = DhcpPacketCodec.Parse(ackBytes);

        Assert.Contains(parsed.Options, o => o.Code == 42);
        Assert.Equal(ntp.GetAddressBytes(), parsed.Options.First(o => o.Code == 42).Data);
    }

    [Fact]
    public void InformAck_Includes_Ntp_When_Configured()
    {
        var req = MakeBasePacket();
        req.Ciaddr = IPAddress.Parse("192.168.1.77");

        var serverId = IPAddress.Parse("192.168.1.1");
        var router = IPAddress.Parse("192.168.1.1");
        var dns = IPAddress.Parse("192.168.1.53");
        var ntp = IPAddress.Parse("192.168.1.123");

        var ackBytes = DhcpPacketCodec.BuildInformAck(
            req,
            serverId,
            router,
            dns,
            ntp);

        var parsed = DhcpPacketCodec.Parse(ackBytes);

        Assert.Contains(parsed.Options, o => o.Code == 42);
    }

    [Fact]
    public void InformAck_Does_Not_Include_Ntp_When_Not_Configured()
    {
        var req = MakeBasePacket();
        req.Ciaddr = IPAddress.Parse("192.168.1.77");

        var serverId = IPAddress.Parse("192.168.1.1");
        var router = IPAddress.Parse("192.168.1.1");
        var dns = IPAddress.Parse("192.168.1.53");

        var ackBytes = DhcpPacketCodec.BuildInformAck(
            req,
            serverId,
            router,
            dns,
            null);

        var parsed = DhcpPacketCodec.Parse(ackBytes);

        Assert.DoesNotContain(parsed.Options, o => o.Code == 42);
    }
}
