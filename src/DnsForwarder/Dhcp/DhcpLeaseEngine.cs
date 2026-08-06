using System.Net;

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

    public DhcpLease? GetLease(PhysicalAddress mac)
    {
        return _store.GetActiveLeases().FirstOrDefault(l => l.Mac.Equals(mac));
    }

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

    public void Release(PhysicalAddress mac)
    {
        _store.Remove(mac);
    }
}
