namespace DnsForwarder.Events;

public interface INtpMetrics
{
    void Sync(NtpSyncEvent evt);
}
