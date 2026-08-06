namespace DnsForwarder.Events;

public sealed class DhcpMetrics : IDhcpMetrics
{
    private readonly EventBus _bus;

    public DhcpMetrics(EventBus bus)
    {
        _bus = bus;
    }

    public void LeaseAllocated(DhcpLeaseAllocatedEvent evt)
        => _bus.Publish(evt);

    public void LeaseReleased(DhcpLeaseReleasedEvent evt)
        => _bus.Publish(evt);

    public void NakSent(DhcpNakEvent evt)
        => _bus.Publish(evt);
}
