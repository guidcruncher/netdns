using System.Net;
using System.Net.NetworkInformation;

namespace DnsForwarder.Dhcp;

public sealed class DhcpLeaseEngine
{
    private readonly IDhcpLeaseStore _store;
    private readonly CidrPoolAllocator _pool;

    // Optional: track IPs that clients DECLINE
    private readonly HashSet<IPAddress> _badIps = new();

    public DhcpLeaseEngine(IDhcpLeaseStore store, CidrPoolAllocator pool)
    {
        _store = store;
        _pool = pool;
    }

    // ------------------------------------------------------------
    // GET EXISTING LEASE
    // ------------------------------------------------------------
    public DhcpLease? GetLease(PhysicalAddress mac)
    {
        return _store.GetActiveLeases().FirstOrDefault(l => l.Mac.Equals(mac));
    }

    // ------------------------------------------------------------
    // BASIC ALLOCATION (no ARP check)
    // ------------------------------------------------------------
    public DhcpLease Allocate(PhysicalAddress mac, TimeSpan leaseTime)
    {
        var existing = GetLease(mac);
        if (existing != null)
        {
            existing.ExpiresAt = DateTimeOffset.UtcNow.Add(leaseTime);
            _store.Save(existing);
            return existing;
        }

        var ip = _pool.Allocate(_store.GetActiveLeases().Select(l => l.Ip));
        if (ip == null)
            throw new Exception("DHCP pool exhausted");

        var lease = new DhcpLease
        {
            Mac = mac,
            Ip = ip,
            ExpiresAt = DateTimeOffset.UtcNow.Add(leaseTime)
        };

        _store.Save(lease);
        return lease;
    }

    // ------------------------------------------------------------
    // ADVANCED ALLOCATION WITH ARP CONFLICT DETECTION
    // ------------------------------------------------------------
    public async Task<DhcpLease> AllocateWithArpCheck(
        PhysicalAddress mac,
        TimeSpan leaseTime,
        ArpConflictDetector arp)
    {
        // Renew existing lease
        var existing = GetLease(mac);
        if (existing != null)
        {
            existing.ExpiresAt = DateTimeOffset.UtcNow.Add(leaseTime);
            _store.Save(existing);
            return existing;
        }

        // Try each candidate IP in pool
        var used = _store.GetActiveLeases().Select(l => l.Ip);

        foreach (var candidate in _pool.AllocationSequence(used))
        {
            if (_badIps.Contains(candidate))
                continue;

            bool conflict = await arp.HasConflictAsync(candidate, TimeSpan.FromMilliseconds(500));
            if (!conflict)
            {
                var lease = new DhcpLease
                {
                    Mac = mac,
                    Ip = candidate,
                    ExpiresAt = DateTimeOffset.UtcNow.Add(leaseTime)
                };

                _store.Save(lease);
                return lease;
            }
        }

        throw new Exception("DHCP pool exhausted (no conflict-free IPs)");
    }

    // ------------------------------------------------------------
    // RELEASE LEASE
    // ------------------------------------------------------------
    public void Release(PhysicalAddress mac)
    {
        _store.Remove(mac);
    }

    // ------------------------------------------------------------
    // DECLINE HANDLER (mark IP as bad)
    // ------------------------------------------------------------
    public void Decline(IPAddress ip)
    {
        _badIps.Add(ip);
    }
}

