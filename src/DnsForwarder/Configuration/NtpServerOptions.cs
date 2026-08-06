namespace DnsForwarder;

public sealed class NtpServerOptions
{
    public IPAddress ListenAddress { get; set; } = IPAddress.Any;
    public int Port { get; set; } = 123;
    public int BufferSize { get; set; } = 65536;
    public int Stratum { get; set; } = 1;
    public string ReferenceId { get; set; } = "LOCL";
}
