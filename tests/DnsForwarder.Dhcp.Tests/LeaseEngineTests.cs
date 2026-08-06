using System.Net;
using System.Net.NetworkInformation;

using DnsForwarder.Dhcp;

using FluentAssertions;

using Xunit;

public class LeaseEngineTests
{
    private static PhysicalAddress Mac(int id) =>
        new PhysicalAddress(new byte[] { 0, 1, 2, 3, 4, (byte)id });

    [Fact]
    public async Task AllocateWithArpCheck_ShouldAllocateNewLease()
    {
        var store = new InMemoryDhcpLeaseStore();
        var pool = new CidrPoolAllocator("192.168.10.0/29");
        var engine = new DhcpLeaseEngine(store, pool);
        var arp = new ArpConflictDetector(IPAddress.Parse("127.0.0.1"));

        var lease = await engine.AllocateWithArpCheck(Mac(1), TimeSpan.FromHours(1), arp);

        lease.Ip.ToString().Should().Be("192.168.10.1");
        store.GetActiveLeases().Should().ContainSingle();
    }

    [Fact]
    public async Task AllocateWithArpCheck_ShouldRenewExistingLease()
    {
        var store = new InMemoryDhcpLeaseStore();
        var pool = new CidrPoolAllocator("192.168.10.0/29");
        var engine = new DhcpLeaseEngine(store, pool);
        var arp = new ArpConflictDetector(IPAddress.Parse("127.0.0.1"));

        var first = await engine.AllocateWithArpCheck(Mac(1), TimeSpan.FromHours(1), arp);
        var second = await engine.AllocateWithArpCheck(Mac(1), TimeSpan.FromHours(1), arp);

        second.Ip.Should().Be(first.Ip);
    }

    [Fact]
    public void Release_ShouldRemoveLease()
    {
        var store = new InMemoryDhcpLeaseStore();
        var pool = new CidrPoolAllocator("192.168.10.0/29");
        var engine = new DhcpLeaseEngine(store, pool);

        engine.Save(new DhcpLease
        {
            Mac = Mac(1),
            Ip = IPAddress.Parse("192.168.10.1"),
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        });

        engine.Release(Mac(1));

        store.GetActiveLeases().Should().BeEmpty();
    }

    [Fact]
    public void Decline_ShouldQuarantineIp()
    {
        var store = new InMemoryDhcpLeaseStore();
        var pool = new CidrPoolAllocator("192.168.10.0/29");
        var engine = new DhcpLeaseEngine(store, pool);

        var ip = IPAddress.Parse("192.168.10.5");
        engine.Decline(ip);

        store.GetBadIps().Should().Contain(ip);
    }
}
