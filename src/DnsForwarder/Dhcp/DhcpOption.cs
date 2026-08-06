namespace DnsForwarder.Dhcp;

public sealed class DhcpOption
{
    public byte Code { get; }
    public byte[] Data { get; }

    public DhcpOption(byte code, byte[] data)
    {
        Code = code;
        Data = data;
    }
}

