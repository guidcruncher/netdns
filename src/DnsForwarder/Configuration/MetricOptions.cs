using System.Net;

namespace DnsForwarder.Ntp;

public sealed class MetricOptions
{

    public bool Enabled { get; set; } = false;

    public string StorageEngine { get; set; } = "";

    public string Location { get; set; } = "";

    public string ListenAddress { get; set; } = "0.0.0.0";
    public int ListenPort { get; set; } = 1080;

}
