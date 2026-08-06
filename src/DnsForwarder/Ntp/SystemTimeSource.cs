using System;
using System.Threading;
using System.Threading.Tasks;

namespace DnsForwarder.Ntp;

public sealed class SystemTimeSource : INtpTimeSource
{
    private readonly DateTime _ref = DateTime.UtcNow;

    public Task<NtpTimeResult> GetTimeAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        return Task.FromResult(new NtpTimeResult(
            UtcNow: now,
            Offset: TimeSpan.Zero,
            Stratum: 16,            // System clock = unsynchronized
            ReferenceUtc: _ref));
    }
}
