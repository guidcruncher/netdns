namespace DnsForwarder.Dhcp;

public sealed class DhcpOptions
{
    public bool Enabled { get; set; } = false;

    public string ListenAddress { get; set; } = "0.0.0.0";
    public int ListenPort { get; set; } = 67;

    public string LeaseStorePath { get; set; } = "leases.json";
}
