namespace DnsForwarder.Events;

public sealed class NtpMetrics : INtpMetrics
{
    private readonly EventBus _bus;

    public NtpMetrics(EventBus bus)
    {
        _bus = bus;
    }

    public void Sync(NtpSyncEvent evt)
        => _bus.Publish(evt);
}
