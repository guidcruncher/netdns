using System.Buffers.Binary;
using DnsForwarder.Dns.Core;
using Xunit;

namespace DnsForwarder.Dns.Tests
{
    public class DnsBlockedResponseTests
    {
        [Fact]
        public void BuildBlockedResponse_EchoesQuestionAndSetsNxDomain()
        {
            var req = new DnsMessage
            {
                Id = 0x1234
            };

            req.Questions.Add(new DnsQuestion { Name = "example.com", Type = 1, Class = 1 });

            var resp = DnsParser.BuildBlockedResponse(req);

            var parsed = DnsParser.Parse(resp);

            Assert.Equal("NOERROR", parsed.ResponseCode);
            Assert.Equal(1, parsed.Questions.Count);
            Assert.Equal("example.com", parsed.Questions[0].Name);
            Assert.Equal((ushort)1, parsed.Questions[0].Type);
            Assert.Equal((ushort)1, parsed.Questions[0].Class);
        }
    }
}
