using System.Net;
using System.Net.Sockets;

namespace DnsForwarder.Dhcp;

public interface IUdpTransport
{
    Task<UdpReceiveResult> ReceiveAsync(CancellationToken ct);
    Task SendAsync(byte[] buffer, int length, IPEndPoint endpoint);
}
