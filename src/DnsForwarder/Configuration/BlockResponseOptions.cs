namespace DnsForwarder;

public sealed class BlockResponseOptions
{
    public string Mode { get; set; } = "NXDOMAIN";   // NXDOMAIN | SERVFAIL | REFUSED | STATIC_IP
    public string StaticIp { get; set; } = "0.0.0.0";
    public int Ttl { get; set; } = 60;
}
