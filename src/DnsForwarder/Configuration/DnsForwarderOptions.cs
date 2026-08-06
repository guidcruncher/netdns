namespace DnsForwarder.Dns;

public sealed class DnsForwarderOptions
{
    public ListenOptions Listen { get; set; } = new();
    public UpstreamResolverOptions DefaultResolver { get; set; } = new();
    public List<UpstreamResolverOptions> Resolvers { get; set; } = new();
    public CachingOptions Caching { get; set; } = new();

    public List<string> Allowlists { get; set; } = new();
    public List<string> Blocklists { get; set; } = new();

    public List<string> HostsFiles { get; set; } = new();

    public BlockResponseOptions BlockResponse { get; set; } = new();

}

public sealed class ListenOptions
{
    public string Address { get; set; } = "0.0.0.0";
    public int Port { get; set; } = 53;
}

public sealed class UpstreamResolverOptions
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = "1.1.1.1";
    public int Port { get; set; } = 53;
    public string? Rule { get; set; }
    public bool Block { get; set; }
}

public sealed class CachingOptions
{
    public bool Enabled { get; set; } = true;
    public int TtlSeconds { get; set; } = 300;
    public int MaxEntries { get; set; } = 10000;
}
