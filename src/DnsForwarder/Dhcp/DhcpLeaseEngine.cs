using System.Net;
using System.Net.NetworkInformation;

namespace DnsForwarder.Dhcp;

public sealed class DhcpLeaseEngine
{
    private readonly IDhcpLeaseStore _store;
    private readonly CidrPoolAllocator _pool;

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
    // BASIC ALLOCATION
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
    // ADVANCED ALLOCATION WITH ARP CHECK
    // ------------------------------------------------------------
    public async Task<DhcpLease> AllocateWithArpCheck(
        PhysicalAddress mac,
        TimeSpan leaseTime,
        ArpConflictDetector arp)
    {
        var existing = GetLease(mac);
        if (existing != null)
        {
            existing.ExpiresAt = DateTimeOffset.UtcNow.Add(leaseTime);
            _store.Save(existing);
            return existing;
        }

        var used = _store.GetActiveLeases().Select(l => l.Ip);

        foreach (var candidate in _pool.AllocationSequence(used))
        {
            if (_store.GetBadIps().Contains(candidate))
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
    // DECLINE (QUARANTINE IP)
    // ------------------------------------------------------------
    public void Decline(IPAddress ip)
    {
        _store.AddBadIp(ip);
    }
}
