using System.Net;

namespace DnsForwarder.Dns.Core;

public sealed class DnsMessage
{
    public ushort Id { get; set; }
    public bool IsResponse { get; set; }

    public List<DnsQuestion> Questions { get; } = new();
    public List<DnsResourceRecord> Answers { get; } = new();

    // Added for metrics
    public string ResponseCode { get; set; } = "NOERROR";
    public IPAddress? AnswerAddress { get; set; }

    public int GetMinTtl()
    {
        return Answers.Count == 0 ? 60 : Answers.Min(a => a.Ttl);
    }

    // ------------------------------------------------------------
    // Minimal parser for metrics (non-blocking, safe)
    // ------------------------------------------------------------
    public static DnsMessage? TryParse(byte[] buffer)
    {
        try
        {
            var reader = new DnsMessageReader(buffer);
            return reader.Parse();
        }
        catch
        {
            return null;
        }
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

