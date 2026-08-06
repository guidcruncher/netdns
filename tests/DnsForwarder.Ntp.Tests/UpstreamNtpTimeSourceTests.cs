using System;
using System.Reflection;

using DnsForwarder.Ntp;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Xunit;

public class UpstreamNtpTimeSourceTests
{
    private UpstreamNtpTimeSource CreateInstance()
    {
        var options = Options.Create(new NtpServerOptions
        {
            Enabled = true,

            Upstream = new UpstreamNtpOptions
            {
                Enabled = true,
                Servers = new[] { "0.pool.ntp.org" },
                PollIntervalSeconds = 16
            }
        });

        return new UpstreamNtpTimeSource(
            NullLogger<UpstreamNtpTimeSource>.Instance,
            options
        );
    }

    private void InvokeParseResponse(
        UpstreamNtpTimeSource instance,
        byte[] buffer,
        DateTime t1,
        DateTime t4)
    {
        var method = typeof(UpstreamNtpTimeSource)
            .GetMethod("ParseResponse", BindingFlags.NonPublic | BindingFlags.Instance);

        if (method is null)
            throw new InvalidOperationException("ParseResponse method not found.");

        method.Invoke(instance, new object[] { buffer, t1, t4 });
    }

    private TimeSpan GetOffset(UpstreamNtpTimeSource instance)
    {
        var field = typeof(UpstreamNtpTimeSource)
            .GetField("_offset", BindingFlags.NonPublic | BindingFlags.Instance);

        if (field is null)
            throw new InvalidOperationException("_offset field not found.");

        var value = field.GetValue(instance);
        if (value is null)
            throw new InvalidOperationException("_offset is null.");

        return (TimeSpan)value;
    }

    private DateTime GetReferenceUtc(UpstreamNtpTimeSource instance)
    {
        var field = typeof(UpstreamNtpTimeSource)
            .GetField("_referenceUtc", BindingFlags.NonPublic | BindingFlags.Instance);

        if (field is null)
            throw new InvalidOperationException("_referenceUtc field not found.");

        var value = field.GetValue(instance);
        if (value is null)
            throw new InvalidOperationException("_referenceUtc is null.");

        return (DateTime)value;
    }

    private int GetStratum(UpstreamNtpTimeSource instance)
    {
        var field = typeof(UpstreamNtpTimeSource)
            .GetField("_stratum", BindingFlags.NonPublic | BindingFlags.Instance);

        if (field is null)
            throw new InvalidOperationException("_stratum field not found.");

        var value = field.GetValue(instance);
        if (value is null)
            throw new InvalidOperationException("_stratum is null.");

        return (int)value;
    }

    private static byte[] BuildFakeResponse(DateTime t2, DateTime t3, int stratum = 2)
    {
        var buffer = new byte[48];

        buffer[0] = 0b_00100100; // LI=0, VN=4, Mode=4 (server)
        buffer[1] = (byte)stratum;

        NtpTimestamp.WriteTimestamp(buffer, 32, t2);
        NtpTimestamp.WriteTimestamp(buffer, 40, t3);

        return buffer;
    }

    [Fact]
    public void Upstream_ParsesOffsetCorrectly()
    {
        var upstream = CreateInstance();

        var t1 = new DateTime(2024, 1, 1, 12, 00, 00, DateTimeKind.Utc);
        var t2 = t1.AddMilliseconds(10);
        var t3 = t1.AddMilliseconds(15);
        var t4 = t1.AddMilliseconds(25);

        var packet = BuildFakeResponse(t2, t3);

        InvokeParseResponse(upstream, packet, t1, t4);

        var expectedOffset = ((t2 - t1) + (t3 - t4)) / 2;
        Assert.Equal(expectedOffset, GetOffset(upstream));
    }

    [Fact]
    public void Upstream_UpdatesReferenceTimestamp()
    {
        var upstream = CreateInstance();

        var t1 = DateTime.UtcNow;
        var t2 = t1.AddMilliseconds(20);
        var t3 = t1.AddMilliseconds(30);
        var t4 = t1.AddMilliseconds(40);

        var packet = BuildFakeResponse(t2, t3);

        InvokeParseResponse(upstream, packet, t1, t4);

        Assert.Equal(t3, GetReferenceUtc(upstream));
    }

    [Fact]
    public void Upstream_PropagatesStratum()
    {
        var upstream = CreateInstance();

        var t1 = DateTime.UtcNow;
        var t2 = t1.AddMilliseconds(10);
        var t3 = t1.AddMilliseconds(20);
        var t4 = t1.AddMilliseconds(30);

        var packet = BuildFakeResponse(t2, t3, stratum: 5);

        InvokeParseResponse(upstream, packet, t1, t4);

        Assert.Equal(5, GetStratum(upstream));
    }
}
