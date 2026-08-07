namespace DnsForwarder.Events;

public sealed record DnsCacheHitEvent() : EventRecord(DateTime.UtcNow);

