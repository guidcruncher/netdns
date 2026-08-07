
using System.Buffers.Binary;
using System.Net.Sockets;

using DnsForwarder;

using Microsoft.Extensions.Logging;

namespace DnsForwarder.Ntp;

public interface INtpRequestHandler
{
    Task<NtpResponse> HandleAsync(UdpReceiveResult result, UdpClient udp, CancellationToken ct);
}

