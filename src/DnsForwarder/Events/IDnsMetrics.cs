namespace DnsForwarder.Events;

public interface IDnsMetrics
{
    void Query(DnsQueryEvent evt);
    void Response(DnsResponseEvent evt);
    void UpstreamLatency(DnsUpstreamLatencyEvent evt);
}
