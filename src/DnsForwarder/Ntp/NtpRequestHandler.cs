using System.Buffers.Binary;
using System.Net.Sockets;

using DnsForwarder;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DnsForwarder.Ntp;


public sealed class NtpRequestHandler : INtpRequestHandler
{
    private readonly ILogger<NtpRequestHandler> _logger;
    private readonly ITimeSource _timeSource;
    private readonly NtpServerOptions _options;

    public NtpRequestHandler(
        ILogger<NtpRequestHandler> logger,
        ITimeSource timeSource,
        NtpServerOptions options)
    {
        _logger = logger;
        _timeSource = timeSource;
        _options = options;
    }

    public async Task HandleAsync(UdpReceiveResult result, UdpClient udp, CancellationToken ct)
    {
        var buffer = result.Buffer;

        if (buffer.Length < 48)
        {
            _logger.LogWarning("Received invalid NTP packet from {Remote}", result.RemoteEndPoint);
            return;
        }

        byte liVnMode = buffer[0];
        int mode = liVnMode & 0x7;

        if (mode != 3)
        {
            _logger.LogDebug("Ignoring non-client mode packet from {Remote}", result.RemoteEndPoint);
            return;
        }

        var receiveUtc = _timeSource.UtcNow;

        var response = new byte[48];
        Buffer.BlockCopy(buffer, 0, response, 0, 48);

        response[0] = (byte)((0 << 6) | ((liVnMode >> 3) & 0x7) << 3 | 4);
        response[1] = (byte)_options.Stratum;
        response[2] = 4;
        response[3] = unchecked((byte)-20);

        WriteInt32(response, 4, 0);
        WriteInt32(response, 8, 0);

        var refIdBytes = System.Text.Encoding.ASCII.GetBytes(_options.ReferenceId);
        Array.Copy(refIdBytes, 0, response, 12, Math.Min(4, refIdBytes.Length));

        WriteTimestamp(response, 16, _timeSource.ReferenceUtc);
        Buffer.BlockCopy(buffer, 40, response, 24, 8);
        WriteTimestamp(response, 32, receiveUtc);
        WriteTimestamp(response, 40, _timeSource.UtcNow);

        await udp.SendAsync(response, response.Length, result.RemoteEndPoint);

        _logger.LogInformation("Responded to NTP request from {Remote}", result.RemoteEndPoint);
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

