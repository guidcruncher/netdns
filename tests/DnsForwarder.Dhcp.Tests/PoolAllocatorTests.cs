using System.Linq;
using System.Net;

using DnsForwarder.Dhcp;

using Xunit;


namespace DnsForwarder.Dhcp.Tests;

public class PoolAllocatorTests
{
    [Fact]
    public void Allocate_ShouldReturnFirstFreeIp()
    {
        var pool = new CidrPoolAllocator("192.168.10.0/29");

        var ip = pool.Allocate(new IPAddress[0]);

        Assert.NotNull(ip);
        Assert.Equal("192.168.10.1", ip!.ToString());
    }

    [Fact]
    public void AllocationSequence_ShouldEnumerateUsableIps()
    {
        var pool = new CidrPoolAllocator("192.168.10.0/29");

        var seq = pool.AllocationSequence(new IPAddress[0]);

        Assert.Equal(6, seq.Count());
        Assert.Equal("192.168.10.1", seq.First().ToString());
    }
}
