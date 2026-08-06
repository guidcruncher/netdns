using System.Net;
using System.Net.NetworkInformation;

using DnsForwarder.Dhcp;

namespace DnsForwarder.Dhcp.Tests;

public static class PacketFactory
{
    private static readonly byte[] MagicCookie = { 99, 130, 83, 99 };

    private static readonly PhysicalAddress DefaultMac =
        new PhysicalAddress(new byte[] { 0, 1, 2, 3, 4, 5 });

    // ------------------------------------------------------------
    // DISCOVER (object)
    // ------------------------------------------------------------
    public static DhcpPacket Discover()
    {
        var buf = BuildBasePacket(DefaultMac, DhcpMessageType.Discover);
        return DhcpPacketCodec.Parse(buf);
    }

    // ------------------------------------------------------------
    // DISCOVER (bytes)
    // ------------------------------------------------------------
    public static byte[] DiscoverBytes()
    {
        return BuildBasePacket(DefaultMac, DhcpMessageType.Discover);
    }

    // ------------------------------------------------------------
    // REQUEST (object) — REQUIRED BY PacketCodecTests
    // ------------------------------------------------------------
    public static DhcpPacket Request()
    {
        var buf = BuildBasePacket(DefaultMac, DhcpMessageType.Request);
        return DhcpPacketCodec.Parse(buf);
    }

    // ------------------------------------------------------------
    // REQUEST (bytes) — used in integration tests
    // ------------------------------------------------------------
    public static byte[] RequestBytes(DhcpPacket offer)
    {
        var mac = new PhysicalAddress(offer.Chaddr.Take(offer.Hlen).ToArray());
        var requestedIp = offer.Yiaddr;
        var serverId = offer.GetServerIdentifier();

        var buf = BuildBasePacket(mac, DhcpMessageType.Request);

        // Option 50: Requested IP
        if (requestedIp != null)
            buf = AddOption(buf, 50, requestedIp.GetAddressBytes());

        // Option 54: Server Identifier
        if (serverId != null)
            buf = AddOption(buf, 54, serverId.GetAddressBytes());

        return AddEnd(buf);
    }

    // ------------------------------------------------------------
    // INFORM (bytes)
    // ------------------------------------------------------------
    public static byte[] InformBytes(IPAddress ciaddr)
    {
        var buf = BuildBasePacket(DefaultMac, DhcpMessageType.Inform);

        // Set CIADDR
        Array.Copy(ciaddr.GetAddressBytes(), 0, buf, 12, 4);

        return AddEnd(buf);
    }

    // ------------------------------------------------------------
    // Base DHCP packet builder
    // ------------------------------------------------------------
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

        // DHCP Message Type (53)
        buf.Add(53);
        buf.Add(1);
        buf.Add((byte)type);

        return buf.ToArray();
    }

    // ------------------------------------------------------------
    // Add DHCP option
    // ------------------------------------------------------------
    private static byte[] AddOption(byte[] packet, byte code, byte[] data)
    {
        var list = packet.ToList();
        list.Add(code);
        list.Add((byte)data.Length);
        list.AddRange(data);
        return list.ToArray();
    }

    // ------------------------------------------------------------
    // Add END option (255)
    // ------------------------------------------------------------
    private static byte[] AddEnd(byte[] packet)
    {
        var list = packet.ToList();
        list.Add(255);
        return list.ToArray();
    }
}

