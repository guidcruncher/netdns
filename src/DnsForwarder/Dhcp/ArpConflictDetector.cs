using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace DnsForwarder.Dhcp;

public sealed class ArpConflictDetector
{
    private readonly IPAddress _interfaceIp;

    public ArpConflictDetector(IPAddress interfaceIp)
    {
        _interfaceIp = interfaceIp;
    }

    public async Task<bool> HasConflictAsync(IPAddress candidateIp, TimeSpan timeout)
    {
        // Simple heuristic: send ARP request and see if anyone responds.
        // On Linux, you can shell out to `arping`; here we use Ping as a cheap proxy.
        using var ping = new Ping();

        try
        {
            var reply = await ping.SendPingAsync(candidateIp, (int)timeout.TotalMilliseconds);
            // If someone responds, we treat it as a conflict.
            return reply.Status == IPStatus.Success;
        }
        catch
        {
            return false;
        }
    }
}
