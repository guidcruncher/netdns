namespace DnsForwarder.Ntp;

public interface ITimeSource
{
    DateTime UtcNow { get; }
    DateTime ReferenceUtc { get; }
}

