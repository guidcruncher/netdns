using System.Net;
using System.Threading;

namespace DnsForwarder;

public sealed class StaticDnsClient : IDnsClient
{
    private readonly IPAddress _ip;

    public StaticDnsClient(IPAddress ip)
    {
        _ip = ip;
    }

    public Task<byte[]> QueryAsync(byte[] request, CancellationToken ct)
    {
        // Extract transaction ID
        ushort id = (ushort)((request[0] << 8) | request[1]);

        // Parse QNAME
        int offset = 12;
        var labels = new List<string>();

        while (request[offset] != 0)
        {
            int len = request[offset];
            offset++;

            var label = System.Text.Encoding.ASCII.GetString(request, offset, len);
            labels.Add(label);

            offset += len;
        }

        string domain = string.Join(".", labels);

        // Skip null byte
        offset++;

        // Extract QTYPE
        ushort qtype = (ushort)((request[offset] << 8) | request[offset + 1]);

        // Build response
        var response = new List<byte>();

        // Transaction ID
        response.Add((byte)(id >> 8));
        response.Add((byte)(id & 0xFF));

        // Flags: standard response, recursion available
        response.Add(0x81); // QR=1, RD=1
        response.Add(0x80); // RA=1, RCODE=0

        // QDCOUNT = 1
        response.Add(0x00);
        response.Add(0x01);

        // ANCOUNT = 1
        response.Add(0x00);
        response.Add(0x01);

        // NSCOUNT = 0, ARCOUNT = 0
        response.Add(0x00);
        response.Add(0x00);
        response.Add(0x00);
        response.Add(0x00);

        // Copy question section from request
        int questionLength = request.Length - 12;
        response.AddRange(request.Skip(12).Take(questionLength));

        // Answer section
        // NAME: pointer to question (0xC00C)
        response.Add(0xC0);
        response.Add(0x0C);

        // TYPE: A or AAAA
        if (_ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            response.Add(0x00);
            response.Add(0x01); // A
        }
        else
        {
            response.Add(0x00);
            response.Add(0x1C); // AAAA
        }

        // CLASS: IN
        response.Add(0x00);
        response.Add(0x01);

        // TTL = 60 seconds
        response.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x3C });

        // RDLENGTH + RDATA
        var addrBytes = _ip.GetAddressBytes();
        response.Add(0x00);
        response.Add((byte)addrBytes.Length);
        response.AddRange(addrBytes);

        return Task.FromResult(response.ToArray());
    }
}
