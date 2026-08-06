using System.Net;
using System.Text;

namespace DnsForwarder.Dhcp;

public sealed class DhcpPacket
{
    public byte Op { get; set; }
    public byte Htype { get; set; }
    public byte Hlen { get; set; }
    public byte Hops { get; set; }
    public uint Xid { get; set; }
    public ushort Secs { get; set; }
    public ushort Flags { get; set; }
    public IPAddress Ciaddr { get; set; } = IPAddress.Any;
    public IPAddress Yiaddr { get; set; } = IPAddress.Any;
    public IPAddress Siaddr { get; set; } = IPAddress.Any;
    public IPAddress Giaddr { get; set; } = IPAddress.Any;
    public byte[] Chaddr { get; set; } = new byte[16];
    public List<DhcpOption> Options { get; } = new();

    public DhcpMessageType? GetMessageType()
    {
        var opt = Options.FirstOrDefault(o => o.Code == 53);
        return opt == null ? null : (DhcpMessageType)opt.Data[0];
    }

    public IPAddress? GetRequestedIp()
    {
        var opt = Options.FirstOrDefault(o => o.Code == 50);
        return opt == null ? null : new IPAddress(opt.Data);
    }

    public IPAddress? GetServerIdentifier()
    {
        var opt = Options.FirstOrDefault(o => o.Code == 54);
        return opt == null ? null : new IPAddress(opt.Data);
    }

    public string? GetHostName()
    {
        var opt = Options.FirstOrDefault(o => o.Code == 12);
        return opt == null ? null : Encoding.ASCII.GetString(opt.Data);
    }

    public string? GetFqdn()
    {
        var opt = Options.FirstOrDefault(o => o.Code == 81);
        return opt == null ? null : Encoding.ASCII.GetString(opt.Data);
    }

}
