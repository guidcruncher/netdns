namespace DnsForwarder;

public sealed class DnsMessage
{
    public ushort Id { get; set; }
    public bool IsResponse { get; set; }
    public List<DnsQuestion> Questions { get; } = new();
    public List<DnsResourceRecord> Answers { get; } = new();

    public int GetMinTtl()
    {
        return Answers.Count == 0 ? 60 : Answers.Min(a => a.Ttl);
    }
}

public sealed class DnsQuestion
{
    public string Name { get; set; } = string.Empty;
    public ushort Type { get; set; }
    public ushort Class { get; set; }
}

public sealed class DnsResourceRecord
{
    public string Name { get; set; } = string.Empty;
    public ushort Type { get; set; }
    public ushort Class { get; set; }
    public int Ttl { get; set; }
    public byte[] RData { get; set; } = Array.Empty<byte>();
}
