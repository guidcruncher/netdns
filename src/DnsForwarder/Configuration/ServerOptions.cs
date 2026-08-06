using DnsForwarder.Dhcp;
using DnsForwarder.Dns;
using DnsForwarder.Ntp;

namespace DnsForwarder;

public sealed class ServerOptions
{

    public DnsForwarderOptions Dns { get; set; } = new();

    public DhcpOptions Dhcp { get; set; } = new();

    public NtpServerOptions Ntp { get; set; } = new();
}
