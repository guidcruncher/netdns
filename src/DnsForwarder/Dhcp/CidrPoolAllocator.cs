using System.Net;

namespace DnsForwarder.Dhcp;

public sealed class CidrPoolAllocator
{
    private readonly IPAddress _network;
    private readonly IPAddress _netmask;
    private readonly uint _first;
    private readonly uint _last;

    public CidrPoolAllocator(string cidr)
    {
        var parts = cidr.Split('/');
        var ip = IPAddress.Parse(parts[0]);
        var prefix = int.Parse(parts[1]);

        uint mask = prefix == 0 ? 0 : uint.MaxValue << (32 - prefix);
        _netmask = FromUInt32(mask);

        uint net = ToUInt32(ip) & mask;
        _network = FromUInt32(net);

        _first = net + 1;
        _last = (net | ~mask) - 1;
    }

    public IEnumerable<IPAddress> AllocationSequence(IEnumerable<IPAddress> used)
    {
        var usedSet = used.Select(ToUInt32).ToHashSet();

        for (uint i = _first; i <= _last; i++)
        {
            var ip = FromUInt32(i);
            if (!usedSet.Contains(i))
                yield return ip;
        }
    }


    public IPAddress? Allocate(IEnumerable<IPAddress> used)
    {
        var usedSet = used.Select(ToUInt32).ToHashSet();

        for (uint i = _first; i <= _last; i++)
        {
            if (!usedSet.Contains(i))
                return FromUInt32(i);
        }

        return null;
    }

    private static uint ToUInt32(IPAddress ip)
    {
        var bytes = ip.GetAddressBytes();
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return BitConverter.ToUInt32(bytes, 0);
    }

    private static IPAddress FromUInt32(uint value)
    {
        var bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return new IPAddress(bytes);
    }
}
