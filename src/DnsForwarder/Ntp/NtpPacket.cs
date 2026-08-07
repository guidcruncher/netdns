using System.Net;

namespace DnsForwarder.Ntp;

public sealed class NtpPacket
{
    public uint TransmitTimestampSeconds { get; set; }
    public uint TransmitTimestampFraction { get; set; }

    public static NtpPacket Parse(byte[] buffer)
    {
        // Minimal parser: only extracts transmit timestamp
        return new NtpPacket
        {
            TransmitTimestampSeconds = ReadUInt32(buffer, 40),
            TransmitTimestampFraction = ReadUInt32(buffer, 44)
        };
    }

    public static NtpPacket BuildResponse(NtpPacket request, DateTime utcNow, TimeSpan offset)
    {
        var unix = (uint)(utcNow - DateTime.UnixEpoch).TotalSeconds;

        return new NtpPacket
        {
            TransmitTimestampSeconds = unix,
            TransmitTimestampFraction = 0
        };
    }

    public byte[] ToBytes()
    {
        var buffer = new byte[48];
        buffer[0] = 0x1C; // LI=0, Version=4, Mode=4 (server)

        WriteUInt32(buffer, 40, TransmitTimestampSeconds);
        WriteUInt32(buffer, 44, TransmitTimestampFraction);

        return buffer;
    }

    private static uint ReadUInt32(byte[] buf, int offset)
        => (uint)((buf[offset] << 24) | (buf[offset + 1] << 16) | (buf[offset + 2] << 8) | buf[offset + 3]);

    private static void WriteUInt32(byte[] buf, int offset, uint value)
    {
        buf[offset] = (byte)(value >> 24);
        buf[offset + 1] = (byte)(value >> 16);
        buf[offset + 2] = (byte)(value >> 8);
        buf[offset + 3] = (byte)value;
    }
}
