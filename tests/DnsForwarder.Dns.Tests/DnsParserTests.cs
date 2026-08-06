using DnsForwarder.Dns;
using DnsForwarder.Dns.Core;

using Xunit;

namespace DnsForwarder.Dns.Tests;

public class DnsParserTests
{
    [Fact]
    public void Parse_Throws_On_Short_Message()
    {
        var buffer = new byte[5];
        Assert.Throws<InvalidOperationException>(() => DnsParser.Parse(buffer));
    }

    [Fact]
    public void Parse_Parses_Header_And_Question()
    {
        // A minimal DNS query for "example.com" type A
        byte[] query =
        {
            0x12, 0x34,             // ID
            0x01, 0x00,             // Flags
            0x00, 0x01,             // QDCOUNT = 1
            0x00, 0x00,             // ANCOUNT
            0x00, 0x00,             // NSCOUNT
            0x00, 0x00,             // ARCOUNT

            // Question: example.com
            0x07, (byte)'e',(byte)'x',(byte)'a',(byte)'m',(byte)'p',(byte)'l',(byte)'e',
            0x03, (byte)'c',(byte)'o',(byte)'m',
            0x00,                   // end of name
            0x00, 0x01,             // Type A
            0x00, 0x01              // Class IN
        };

        var msg = DnsParser.Parse(query);

        Assert.Equal(0x1234, msg.Id);
        Assert.False(msg.IsResponse);
        Assert.Single(msg.Questions);

        var q = msg.Questions[0];
        Assert.Equal("example.com", q.Name);
        Assert.Equal(1, q.Type);
        Assert.Equal(1, q.Class);
    }
}
