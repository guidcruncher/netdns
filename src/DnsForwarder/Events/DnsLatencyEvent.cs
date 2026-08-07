namespace DnsForwarder.Events;

public sealed record DnsLatencyEvent(double Seconds) : EventRecord(DateTime.UtcNow);
