using System;
using Xunit;
using DnsForwarder.Ntp;

public class TimestampTests
{
    [Fact]
    public void WriteTimestamp_EncodesCorrectNtpEpoch()
    {
        var buffer = new byte[48];
        var utc = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        NtpRequestHandler.WriteTimestamp(buffer, 16, utc);

        uint seconds = BitConverter.ToUInt32(buffer[16..20].Reverse().ToArray());
        uint fraction = BitConverter.ToUInt32(buffer[20..24].Reverse().ToArray());

        Assert.True(seconds > 0);
        Assert.True(fraction >= 0);
    }

    [Fact]
    public void WriteTimestamp_ProducesIncreasingValues()
    {
        var buffer = new byte[48];

        var t1 = DateTime.UtcNow;
        var t2 = t1.AddSeconds(1);

        NtpRequestHandler.WriteTimestamp(buffer, 16, t1);
        uint s1 = BitConverter.ToUInt32(buffer[16..20].Reverse().ToArray());

        NtpRequestHandler.WriteTimestamp(buffer, 16, t2);
        uint s2 = BitConverter.ToUInt32(buffer[16..20].Reverse().ToArray());

        Assert.True(s2 > s1);
    }
}
