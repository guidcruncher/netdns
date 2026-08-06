using System;
using System.Buffers.Binary;

namespace DnsForwarder.Ntp;

public static class NtpTimestamp
{
    private static readonly DateTime Epoch =
        new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static void WriteTimestamp(byte[] buffer, int offset, DateTime utc)
    {
        if (utc.Kind != DateTimeKind.Utc)
            utc = utc.ToUniversalTime();

        var span = utc - Epoch;

        ulong seconds = (ulong)span.TotalSeconds;
        double fraction = span.TotalSeconds - Math.Floor(span.TotalSeconds);
        ulong frac = (ulong)(fraction * 0x100000000L);

        BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(offset, 4), (uint)seconds);
        BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(offset + 4, 4), (uint)frac);
    }
}

