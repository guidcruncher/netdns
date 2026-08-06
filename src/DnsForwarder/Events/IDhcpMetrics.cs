namespace DnsForwarder.Events;

public interface IDhcpMetrics
{
    void LeaseAllocated(DhcpLeaseAllocatedEvent evt);
    void LeaseReleased(DhcpLeaseReleasedEvent evt);
    void NakSent(DhcpNakEvent evt);
}
