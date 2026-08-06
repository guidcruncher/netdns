using System.Net;

using DnsForwarder.Dhcp;

using FluentAssertions;

using Xunit;


namespace DnsForwarder.Dhcp.Tests;

public class PoolAllocatorTests
{
    [Fact]
    public void Allocate_ShouldReturnFirstFreeIp()
    {
        var pool = new CidrPoolAllocator("192.168.10.0/29");

        var ip = pool.Allocate(new IPAddress[0]);

        ip.Should().NotBeNull();
        ip!.ToString().Should().Be("192.168.10.1");
    }

    [Fact]
    public void AllocationSequence_ShouldEnumerateUsableIps()
    {
        var pool = new CidrPoolAllocator("192.168.10.0/29");

        var seq = pool.AllocationSequence(new IPAddress[0]);

        seq.Should().HaveCount(6);
        seq.First().ToString().Should().Be("192.168.10.1");
    }
}
