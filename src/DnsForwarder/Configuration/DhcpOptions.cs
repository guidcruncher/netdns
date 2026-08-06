namespace DnsForwarder.Dhcp;

public sealed class DhcpOptions
{
    //
    // Core enable/disable
    //
    public bool Enabled { get; set; } = false;

    //
    // Listener configuration
    //
    public string ListenAddress { get; set; } = "0.0.0.0";
    public int ListenPort { get; set; } = 67;

    //
    // Lease store persistence
    //
    public string LeaseStorePath { get; set; } = "leases.json";

    //
    // Pool configuration (CIDR)
    //
    public string PoolCidr { get; set; } = "192.168.10.0/24";

    //
    // DHCP server identifier (siaddr / option 54)
    //
    public string ServerIdentifier { get; set; } = "192.168.10.1";

    //
    // Router (default gateway) option (3)
    //
    public string Router { get; set; } = "192.168.10.1";

    //
    // DNS server option (6)
    //
    public string DnsServer { get; set; } = "1.1.1.1";

    public string NtpServer { get; set; } = "";

    //
    // Default lease duration
    //
    public int LeaseHours { get; set; } = 1;

    //
    // Optional: ARP conflict detection timeout (ms)
    //
    public int ArpTimeoutMs { get; set; } = 500;

    //
    // Optional: bad IP quarantine persistence
    //
    public string BadIpStorePath { get; set; } = "badips.json";
}
