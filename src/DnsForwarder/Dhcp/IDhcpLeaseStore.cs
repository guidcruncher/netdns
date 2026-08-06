using System.Net;
using System.Net.NetworkInformation;

namespace DnsForwarder.Dhcp;

public interface IDhcpLeaseStore
{
    //
    // Load all persisted data (leases + bad IPs)
    //
    Task LoadAsync();

    //
    // Save all persisted data
    //
    Task SaveAsync();

    //
    // Active leases
    //
    IEnumerable<DhcpLease> GetActiveLeases();

    //
    // Save or update a lease
    //
    void Save(DhcpLease lease);

    //
    // Remove lease by MAC
    //
    void Remove(PhysicalAddress mac);

    //
    // Bad IP quarantine list (from DECLINE)
    //
    IEnumerable<IPAddress> GetBadIps();

    void AddBadIp(IPAddress ip);

    void RemoveBadIp(IPAddress ip);
}
