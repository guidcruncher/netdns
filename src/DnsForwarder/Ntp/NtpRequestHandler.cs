using System.Buffers.Binary;
using System.Net.Sockets;

using DnsForwarder;
using DnsForwarder.Ntp;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DnsForwarder.Ntp;

public sealed class NtpRequestHandler : INtpRequestHandler
{
    private readonly ILogger<NtpRequestHandler> _logger;
    private readonly INtpTimeSource _timeSource;

    public NtpRequestHandler(
        ILogger<NtpRequestHandler> logger,
        INtpTimeSource timeSource)
    {
        _logger = logger;
        _timeSource = timeSource;
    }

    public async Task<NtpResponse> HandleAsync(
        UdpReceiveResult result,
        UdpClient udp,
        CancellationToken ct)
    {
        try
        {
            // Parse request
            var request = NtpPacket.Parse(result.Buffer);

            // Get upstream time
            var upstream = await _timeSource.GetTimeAsync(ct);

            // Build response packet
            var responsePacket = NtpPacket.BuildResponse(
                request,
                upstream.UtcNow,
                upstream.Offset);

            var bytes = responsePacket.ToBytes();

            return new NtpResponse(
                Success: true,
                Offset: upstream.Offset,
                Bytes: bytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to process NTP request from {Remote}",
                result.RemoteEndPoint);

            return new NtpResponse(
                Success: false,
                Offset: TimeSpan.Zero,
                Bytes: null);
        }
    }

    private static void WriteInt32(byte[] buffer, int offset, int value)
    {
        BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(offset, 4), value);
    }

    private static void WriteTimestamp(byte[] buffer, int offset, DateTime utc)
    {
        var epoch = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var span = utc - epoch;

        ulong seconds = (ulong)span.TotalSeconds;
        double fraction = span.TotalSeconds - Math.Floor(span.TotalSeconds);
        ulong frac = (ulong)(fraction * 0x100000000L);

        BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(offset, 4), (uint)seconds);
        BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(offset + 4, 4), (uint)frac);
    }
}

