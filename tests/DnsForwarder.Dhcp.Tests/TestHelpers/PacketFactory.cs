using System.Net;
using System.Net.NetworkInformation;

using DnsForwarder.Dhcp;

namespace DnsForwarder.Dhcp.Tests;

public static class PacketFactory
{
    private static readonly byte[] MagicCookie = { 99, 130, 83, 99 };

    public static DhcpPacket Discover()
    {
        var mac = new PhysicalAddress(new byte[] { 0, 1, 2, 3, 4, 5 });

        var buf = BuildBasePacket(mac, DhcpMessageType.Discover);
        return DhcpPacketCodec.Parse(buf);
    }

    public static byte[] DiscoverBytes()
    {
        var mac = new PhysicalAddress(new byte[] { 0, 1, 2, 3, 4, 5 });
        return BuildBasePacket(mac, DhcpMessageType.Discover);
    }


    public static DhcpPacket Request()
    {
        var mac = new PhysicalAddress(new byte[] { 0, 1, 2, 3, 4, 5 });

        var buf = BuildBasePacket(mac, DhcpMessageType.Request);
        return DhcpPacketCodec.Parse(buf);
    }

    private static byte[] BuildBasePacket(PhysicalAddress mac, DhcpMessageType type)
    {
        var buf = new List<byte>();

        // BOOTREQUEST
        buf.Add(1); // op
        buf.Add(1); // htype
        buf.Add(6); // hlen
        buf.Add(0); // hops

        buf.AddRange(BitConverter.GetBytes((uint)1234)); // xid
        buf.AddRange(new byte[2]); // secs
        buf.AddRange(new byte[2]); // flags

        buf.AddRange(new byte[4]); // ciaddr
        buf.AddRange(new byte[4]); // yiaddr
        buf.AddRange(new byte[4]); // siaddr
        buf.AddRange(new byte[4]); // giaddr

        var macBytes = mac.GetAddressBytes();
        buf.AddRange(macBytes);
        buf.AddRange(new byte[16 - macBytes.Length]); // pad CHADDR

        buf.AddRange(new byte[64]);  // sname
        buf.AddRange(new byte[128]); // file

        buf.AddRange(MagicCookie);

        // DHCP Message Type
        buf.Add(53);
        buf.Add(1);
        buf.Add((byte)type);

        buf.Add(255); // END

        return buf.ToArray();
    }
}
