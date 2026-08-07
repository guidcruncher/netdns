using System;
using System.Buffers.Binary;

using DnsForwarder.Ntp;

using Xunit;

namespace DnsForwarder.Ntp.Tests
{
    public class NtpTimestampTests
    {
        [Fact]
        public void WriteTimestamp_UnixEpochProducesExpectedSeconds()
        {
            var buf = new byte[8];
            var unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            NtpTimestamp.WriteTimestamp(buf, 0, unixEpoch);

            // NTP seconds for 1970-01-01 is 2208988800
            uint seconds = BinaryPrimitives.ReadUInt32BigEndian(buf.AsSpan(0, 4));
            Assert.Equal(2208988800u, seconds);

            uint frac = BinaryPrimitives.ReadUInt32BigEndian(buf.AsSpan(4, 4));
            Assert.Equal(0u, frac);
        }

        [Fact]
        public void WriteTimestamp_HalfSecondFractionProducesCorrectFraction()
        {
            var buf = new byte[8];
            var t = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(500);
            NtpTimestamp.WriteTimestamp(buf, 0, t);

            uint frac = BinaryPrimitives.ReadUInt32BigEndian(buf.AsSpan(4, 4));
            // half second should be 0x80000000
            Assert.Equal(0x80000000u, frac);
        }
    }
}
