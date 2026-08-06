using System.Net;

namespace DnsForwarder.Ntp;

public sealed class MetricOptions
{

    public bool Enabled { get; set; } = false;

    public string StorageEngine { get; set; } = "";
}
