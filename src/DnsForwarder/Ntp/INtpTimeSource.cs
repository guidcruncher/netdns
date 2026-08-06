namespace DnsForwarder.Ntp;

public interface INtpTimeSource
{
    /// <summary>
    /// Returns the current disciplined time and offset.
    /// </summary>
    Task<NtpTimeResult> GetTimeAsync(CancellationToken ct);
}

/// <summary>
/// Result from an NTP time source.
/// </summary>
public sealed record NtpTimeResult(
    DateTime UtcNow,
    TimeSpan Offset,
    int Stratum,
    DateTime ReferenceUtc);
