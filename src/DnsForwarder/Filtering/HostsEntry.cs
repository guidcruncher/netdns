using System.Net;


namespace DnsForwarder.Filtering;

public sealed class HostsEntry
{
    public required string Domain { get; init; }
    public required IPAddress Address { get; init; }
    public required string Source { get; init; }
}
