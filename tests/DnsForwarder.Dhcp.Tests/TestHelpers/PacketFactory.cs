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
    // DISCOVER
    // ------------------------------------------------------------
    public static DhcpPacket Discover()
        => DhcpPacketCodec.Parse(DiscoverBytes());

    public static byte[] DiscoverBytes()
        => AddEnd(BuildBasePacket(DefaultMac, DhcpMessageType.Discover));

    // ------------------------------------------------------------
    // REQUEST (object)
    // ------------------------------------------------------------
    public static DhcpPacket Request()
        => DhcpPacketCodec.Parse(BuildBasePacket(DefaultMac, DhcpMessageType.Request));

    // ------------------------------------------------------------
    // REQUEST (bytes) for integration tests
    // ------------------------------------------------------------
    public static byte[] RequestBytes(DhcpPacket offer)
    {
        var mac = new PhysicalAddress(offer.Chaddr.Take(offer.Hlen).ToArray());
        var pkt = BuildBasePacket(mac, DhcpMessageType.Request);

        // Option 50: Requested IP
        pkt = AddOption(pkt, 50, offer.Yiaddr.GetAddressBytes());

        // Option 54: Server Identifier
        var serverId = offer.GetServerIdentifier();
        if (serverId != null)
            pkt = AddOption(pkt, 54, serverId.GetAddressBytes());

        return AddEnd(pkt);
    }

    // ------------------------------------------------------------
    // INFORM
    // ------------------------------------------------------------
    public static byte[] InformBytes(IPAddress ciaddr)
    {
        var pkt = BuildBasePacket(DefaultMac, DhcpMessageType.Inform);

        // Set CIADDR
        Array.Copy(ciaddr.GetAddressBytes(), 0, pkt, 12, 4);

        return AddEnd(pkt);
    }

    // ------------------------------------------------------------
    // DECLINE
    // ------------------------------------------------------------
    public static byte[] DeclineBytes(DhcpPacket offer)
    {
        var mac = new PhysicalAddress(offer.Chaddr.Take(offer.Hlen).ToArray());
        var pkt = BuildBasePacket(mac, DhcpMessageType.Decline);

        // Option 50: Requested IP (the IP being declined)
        pkt = AddOption(pkt, 50, offer.Yiaddr.GetAddressBytes());

        return AddEnd(pkt);
    }

    // ------------------------------------------------------------
    // RELEASE
    // ------------------------------------------------------------
    public static byte[] ReleaseBytes(DhcpPacket offer)
    {
        var mac = new PhysicalAddress(offer.Chaddr.Take(offer.Hlen).ToArray());
        var pkt = BuildBasePacket(mac, DhcpMessageType.Release);

        // RELEASE sets CIADDR to the client's leased IP
        Array.Copy(offer.Yiaddr.GetAddressBytes(), 0, pkt, 12, 4);

        return AddEnd(pkt);
    }

    // ------------------------------------------------------------
    // Base DHCP packet builder
    // ------------------------------------------------------------
    private static byte[] BuildBasePacket(PhysicalAddress mac, DhcpMessageType type)
    {
        var buf = new List<byte>();

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
    // END option
    // ------------------------------------------------------------
    private static byte[] AddEnd(byte[] packet)
    {
        var list = packet.ToList();
        list.Add(255);
        return list.ToArray();
    }
}
