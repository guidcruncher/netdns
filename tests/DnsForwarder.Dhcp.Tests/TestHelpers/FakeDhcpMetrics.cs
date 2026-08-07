using DnsForwarder.Events;

public sealed class FakeDhcpMetrics : IDhcpMetrics
{
    public void LeaseAllocated(DhcpLeaseAllocatedEvent evt) { }
    public void LeaseReleased(DhcpLeaseReleasedEvent evt) { }
    public void NakSent(DhcpNakEvent evt) { }
}
