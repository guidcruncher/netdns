namespace DnsForwarder.Ntp;

public sealed class SystemTimeSource : ITimeSource
{
    private readonly DateTime _ref = DateTime.UtcNow;

    public DateTime UtcNow => DateTime.UtcNow;
    public DateTime ReferenceUtc => _ref;
}


