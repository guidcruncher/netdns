using System.Net;
using System.Net.NetworkInformation;

namespace DnsForwarder.Dhcp;

public sealed class InMemoryDhcpLeaseStore : IDhcpLeaseStore
{
    private readonly Dictionary<PhysicalAddress, DhcpLease> _leases = new();
    private readonly HashSet<IPAddress> _badIps = new();

    // ------------------------------------------------------------
    // LOAD (no-op for in-memory)
    // ------------------------------------------------------------
    public Task LoadAsync()
    {
        return Task.CompletedTask;
    }

    // ------------------------------------------------------------
    // SAVE (no-op for in-memory)
    // ------------------------------------------------------------
    public Task SaveAsync()
    {
        return Task.CompletedTask;
    }

    // ------------------------------------------------------------
    // ACTIVE LEASES
    // ------------------------------------------------------------
    public IEnumerable<DhcpLease> GetActiveLeases()
    {
        var now = DateTimeOffset.UtcNow;
        return _leases.Values.Where(l => l.ExpiresAt > now);
    }

    // ------------------------------------------------------------
    // SAVE / UPDATE LEASE
    // ------------------------------------------------------------
    public void Save(DhcpLease lease)
    {
        _leases[lease.Mac] = lease;
    }

    // ------------------------------------------------------------
    // REMOVE LEASE
    // ------------------------------------------------------------
    public void Remove(PhysicalAddress mac)
    {
        _leases.Remove(mac);
    }

    // ------------------------------------------------------------
    // BAD IPs
    // ------------------------------------------------------------
    public IEnumerable<IPAddress> GetBadIps() => _badIps;

    public void AddBadIp(IPAddress ip)
    {
        _badIps.Add(ip);
    }

    public void RemoveBadIp(IPAddress ip)
    {
        _badIps.Remove(ip);
    }
}
