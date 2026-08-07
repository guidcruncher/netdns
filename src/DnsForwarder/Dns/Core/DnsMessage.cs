using System.Net;

namespace DnsForwarder.Dns.Core;

public sealed class DnsMessage
{
    public ushort Id { get; set; }
    public bool IsResponse { get; set; }

    public List<DnsQuestion> Questions { get; } = new();
    public List<DnsResourceRecord> Answers { get; } = new();

    // Metrics fields
    public string ResponseCode { get; set; } = "NOERROR";
    public IPAddress? AnswerAddress { get; set; }

    // Convenience properties for DNS server + metrics
    public string QuestionName
        => Questions.Count > 0 ? Questions[0].Name : string.Empty;

    public string QuestionType
        => Questions.Count > 0 ? Questions[0].Type.ToString() : string.Empty;

    public int GetMinTtl()
        => Answers.Count == 0 ? 60 : Answers.Min(a => a.Ttl);

    public static DnsMessage? TryParse(byte[] buffer)
    {
        try
        {
            return DnsParser.Parse(buffer);
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
