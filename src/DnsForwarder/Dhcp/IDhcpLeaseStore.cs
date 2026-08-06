namespace DnsForwarder.Dhcp;

public interface IDhcpLeaseStore
{
    Task LoadAsync();
    Task SaveAsync();
}
