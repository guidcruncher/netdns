using System.Net;
using System.Text.RegularExpressions;

using DnsForwarder.Dns.Core;
using DnsForwarder.Dns.Filtering;
using DnsForwarder.Utils;

using Microsoft.Extensions.Logging;

namespace DnsForwarder.Dns.RuleEngine;

internal sealed class BlockResponseBuilder
{
    private readonly DnsForwarderOptions _options;

    public BlockResponseBuilder(DnsForwarderOptions options)
    {
        _options = options;
    }

    public byte[] BuildBlockResponse(byte[] request)
    {
        var mode = _options.BlockResponse.Mode.ToUpperInvariant();

        return mode switch
        {
            "NXDOMAIN" => BuildRcodeResponse(request, rcode: 3),
            "SERVFAIL" => BuildRcodeResponse(request, rcode: 2),
            "REFUSED" => BuildRcodeResponse(request, rcode: 5),
            "STATIC_IP" => BuildStaticIpResponse(request, IPAddress.Parse(_options.BlockResponse.StaticIp)),
            _ => BuildRcodeResponse(request, rcode: 3)
        };
    }

    private static byte[] BuildRcodeResponse(byte[] req, int rcode)
    {
        var resp = new List<byte>
        {
            req[0], req[1],
            0x81, (byte)(0x80 | (rcode & 0x0F)),
            0x00, 0x01,
            0x00, 0x00,
            0x00, 0x00,
            0x00, 0x00
        };

        resp.AddRange(req.Skip(12));
        return resp.ToArray();
    }

    private byte[] BuildStaticIpResponse(byte[] req, IPAddress ip)
    {
        ushort id = (ushort)((req[0] << 8) | req[1]);

        var response = new List<byte>
        {
            (byte)(id >> 8), (byte)(id & 0xFF),
            0x81, 0x80,
            0x00, 0x01,
            0x00, 0x01,
            0x00, 0x00,
            0x00, 0x00
        };

        response.AddRange(req.Skip(12));

        response.Add(0xC0);
        response.Add(0x0C);

        var addrBytes = ip.GetAddressBytes();

        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            response.AddRange(new byte[] { 0x00, 0x01 });
        else
            response.AddRange(new byte[] { 0x00, 0x1C });

        response.AddRange(new byte[] { 0x00, 0x01 });

        var ttlBytes = new byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(ttlBytes, _options.BlockResponse.Ttl);
        response.AddRange(ttlBytes);

        response.Add(0x00);
        response.Add((byte)addrBytes.Length);
        response.AddRange(addrBytes);

        return response.ToArray();
    }

    public static byte[] BuildServfail(byte[] req)
    {
        var r = new List<byte>
        {
            req[0], req[1],
            0x81, 0x82,
            0x00, 0x01,
            0x00, 0x00,
            0x00, 0x00,
            0x00, 0x00
        };

        r.AddRange(req.Skip(12));
        return r.ToArray();
    }
}
