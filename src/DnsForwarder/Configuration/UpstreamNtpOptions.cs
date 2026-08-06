using System.Net;

namespace DnsForwarder.Ntp;

public sealed class UpstreamNtpOptions
{
    public bool Enabled { get; set; } = true;

    public string[] Servers { get; set; } =
    [
        "0.pool.ntp.org",
        "1.pool.ntp.org"
    ];

    public int PollIntervalSeconds { get; set; } = 16;
}
