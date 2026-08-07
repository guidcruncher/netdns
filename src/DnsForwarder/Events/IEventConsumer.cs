namespace DnsForwarder.Events;

public interface IEventConsumer
{
    void Consume(EventRecord evt);
}
