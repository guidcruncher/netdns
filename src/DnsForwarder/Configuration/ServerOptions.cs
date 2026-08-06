namespace DnsForwarder;

public sealed class ServerOptions
{

    public DnsForwarderOptions Dns { get; set; } = new();

    public DhcpOptions Dhcp { get; set; } = new();

}
