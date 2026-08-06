using System.Net;

namespace DnsForwarder.Dhcp;

public static class DhcpPacketCodec
{
    private static readonly byte[] MagicCookie = { 99, 130, 83, 99 };

    // ------------------------------------------------------------
    // INFORM ACK (no lease, no yiaddr)
    // ------------------------------------------------------------
    public static byte[] BuildInformAck(
        DhcpPacket inform,
        IPAddress serverId,
        IPAddress router,
        IPAddress dns,
        IPAddress? ntp = null)
    {
        var buf = new List<byte>();

        buf.Add(2); // BOOTREPLY
        buf.Add(inform.Htype);
        buf.Add(inform.Hlen);
        buf.Add(inform.Hops);

        buf.AddRange(BitConverter.GetBytes(inform.Xid));
        buf.AddRange(BitConverter.GetBytes(inform.Secs));
        buf.AddRange(BitConverter.GetBytes(inform.Flags));

        buf.AddRange(inform.Ciaddr.GetAddressBytes()); // client already has IP
        buf.AddRange(IPAddress.Any.GetAddressBytes()); // yiaddr = 0
        buf.AddRange(serverId.GetAddressBytes());
        buf.AddRange(inform.Giaddr.GetAddressBytes());

        buf.AddRange(inform.Chaddr);
        buf.AddRange(new byte[64]);
        buf.AddRange(new byte[128]);

        buf.AddRange(MagicCookie);

        // DHCP Message Type = ACK
        buf.Add(53);
        buf.Add(1);
        buf.Add((byte)DhcpMessageType.Ack);

        // Server Identifier
        buf.Add(54);
        buf.Add(4);
        buf.AddRange(serverId.GetAddressBytes());

        // Router
        buf.Add(3);
        buf.Add(4);
        buf.AddRange(router.GetAddressBytes());

        // DNS
        buf.Add(6);
        buf.Add(4);
        buf.AddRange(dns.GetAddressBytes());

        // NTP (optional)
        if (ntp is not null)
        {
            buf.Add(42);
            buf.Add(4);
            buf.AddRange(ntp.GetAddressBytes());
        }

        buf.Add(255); // END

        return buf.ToArray();
    }

    // ------------------------------------------------------------
    // NAK
    // ------------------------------------------------------------
    public static byte[] BuildNak(DhcpPacket request, IPAddress serverId)
    {
        var buf = new List<byte>();

        buf.Add(2);
        buf.Add(request.Htype);
        buf.Add(request.Hlen);
        buf.Add(request.Hops);

        buf.AddRange(BitConverter.GetBytes(request.Xid));
        buf.AddRange(BitConverter.GetBytes(request.Secs));
        buf.AddRange(BitConverter.GetBytes(request.Flags));

        buf.AddRange(request.Ciaddr.GetAddressBytes());
        buf.AddRange(IPAddress.Any.GetAddressBytes());
        buf.AddRange(serverId.GetAddressBytes());
        buf.AddRange(request.Giaddr.GetAddressBytes());

        buf.AddRange(request.Chaddr);
        buf.AddRange(new byte[64]);
        buf.AddRange(new byte[128]);

        buf.AddRange(MagicCookie);

        buf.Add(53);
        buf.Add(1);
        buf.Add((byte)DhcpMessageType.Nak);

        buf.Add(54);
        buf.Add(4);
        buf.AddRange(serverId.GetAddressBytes());

        buf.Add(255);

        return buf.ToArray();
    }

    // ------------------------------------------------------------
    // PARSE DHCP PACKET
    // ------------------------------------------------------------
    public static DhcpPacket Parse(byte[] data)
    {
        var p = new DhcpPacket
        {
            Op = data[0],
            Htype = data[1],
            Hlen = data[2],
            Hops = data[3],
            Xid = BitConverter.ToUInt32(data, 4),
            Secs = BitConverter.ToUInt16(data, 8),
            Flags = BitConverter.ToUInt16(data, 10),
            Ciaddr = new IPAddress(data.Skip(12).Take(4).ToArray()),
            Yiaddr = new IPAddress(data.Skip(16).Take(4).ToArray()),
            Siaddr = new IPAddress(data.Skip(20).Take(4).ToArray()),
            Giaddr = new IPAddress(data.Skip(24).Take(4).ToArray()),
            Chaddr = data.Skip(28).Take(16).ToArray()
        };

        int offset = 236;

        if (!data.Skip(offset).Take(4).SequenceEqual(MagicCookie))
            throw new Exception("Invalid DHCP magic cookie");

        offset += 4;

        while (offset < data.Length)
        {
            byte code = data[offset++];

            if (code == 255)
                break;

            if (code == 0)
                continue;

            byte len = data[offset++];
            var optData = data.Skip(offset).Take(len).ToArray();
            offset += len;

            p.Options.Add(new DhcpOption(code, optData));
        }

        return p;
    }

    // ------------------------------------------------------------
    // OFFER
    // ------------------------------------------------------------
    public static byte[] BuildOffer(
        DhcpPacket discover,
        IPAddress offeredIp,
        IPAddress serverId,
        IPAddress router,
        IPAddress dns,
        IPAddress? ntp,
        TimeSpan lease)
    {
        return BuildResponse(
            discover,
            DhcpMessageType.Offer,
            offeredIp,
            serverId,
            router,
            dns,
            ntp,
            lease);
    }

    // ------------------------------------------------------------
    // ACK
    // ------------------------------------------------------------
    public static byte[] BuildAck(
        DhcpPacket request,
        IPAddress assignedIp,
        IPAddress serverId,
        IPAddress router,
        IPAddress dns,
        IPAddress? ntp,
        TimeSpan lease)
    {
        return BuildResponse(
            request,
            DhcpMessageType.Ack,
            assignedIp,
            serverId,
            router,
            dns,
            ntp,
            lease);
    }

    // ------------------------------------------------------------
    // INTERNAL RESPONSE BUILDER
    // ------------------------------------------------------------
    private static byte[] BuildResponse(
        DhcpPacket req,
        DhcpMessageType type,
        IPAddress yiaddr,
        IPAddress serverId,
        IPAddress router,
        IPAddress dns,
        IPAddress? ntp,
        TimeSpan lease)
    {
        var buf = new List<byte>();

        buf.Add(2);
        buf.Add(req.Htype);
        buf.Add(req.Hlen);
        buf.Add(req.Hops);

        buf.AddRange(BitConverter.GetBytes(req.Xid));
        buf.AddRange(BitConverter.GetBytes(req.Secs));
        buf.AddRange(BitConverter.GetBytes(req.Flags));

        buf.AddRange(req.Ciaddr.GetAddressBytes());
        buf.AddRange(yiaddr.GetAddressBytes());
        buf.AddRange(serverId.GetAddressBytes());
        buf.AddRange(req.Giaddr.GetAddressBytes());

        buf.AddRange(req.Chaddr);
        buf.AddRange(new byte[64]);
        buf.AddRange(new byte[128]);

        buf.AddRange(MagicCookie);

        buf.Add(53);
        buf.Add(1);
        buf.Add((byte)type);

        buf.Add(54);
        buf.Add(4);
        buf.AddRange(serverId.GetAddressBytes());

        buf.Add(51);
        buf.Add(4);
        buf.AddRange(BitConverter.GetBytes((uint)lease.TotalSeconds).Reverse());

        buf.Add(3);
        buf.Add(4);
        buf.AddRange(router.GetAddressBytes());

        buf.Add(6);
        buf.Add(4);
        buf.AddRange(dns.GetAddressBytes());

        // NTP (optional)
        if (ntp is not null)
        {
            buf.Add(42);
            buf.Add(4);
            buf.AddRange(ntp.GetAddressBytes());
        }

        buf.Add(1);
        buf.Add(4);
        buf.AddRange(new byte[] { 255, 255, 255, 0 });

        buf.Add(255);

        return buf.ToArray();
    }
}
