using System.Linq;
using System.Net;
using DnsForwarder.Dhcp;
using Xunit;

namespace DnsForwarder.Dhcp.Tests
{
    public class DhcpOptionsEdgeCasesTests
    {
        [Fact]
        public void BuildOffer_ContainsExpectedOptions_AndParserSkipsPad()
        {
            var discover = PacketFactory.Discover();
            var offer = DhcpPacketCodec.BuildOffer(
                discover,
                IPAddress.Parse("192.168.10.50"),
                IPAddress.Parse("192.168.10.1"),
                IPAddress.Parse("192.168.10.1"),
                IPAddress.Parse("1.1.1.1"),
                null,
                System.TimeSpan.FromHours(1));

            // Insert a PAD (0) before the END to ensure parser skips it
            var list = offer.ToList();
            int endIndex = list.LastIndexOf(255);
            list.Insert(endIndex, 0); // PAD
            var modified = list.ToArray();

            var parsed = DhcpPacketCodec.Parse(modified);

            // Assert essential options present
            var codes = parsed.Options.Select(o => o.Code).ToArray();
            Assert.Contains((byte)53, codes); // message type
            Assert.Contains((byte)54, codes); // server id
            Assert.Contains((byte)3, codes);  // router
            Assert.Contains((byte)6, codes);  // dns
        }
    }
}
