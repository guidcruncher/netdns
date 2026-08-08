using System.Net;
using System.Text.RegularExpressions;

using DnsForwarder.Dns.Core;
using DnsForwarder.Dns.Filtering;
using DnsForwarder.Utils;

using Microsoft.Extensions.Logging;

namespace DnsForwarder.Dns.RuleEngine;

internal static class TtlExtractor
{
    public static int ExtractTtl(byte[] msg)
    {
        int qd = (msg[4] << 8) | msg[5];
        int an = (msg[6] << 8) | msg[7];

        int offset = 12;

        for (int i = 0; i < qd; i++)
            offset = SkipName(msg, offset) + 4;

        int min = int.MaxValue;

        for (int i = 0; i < an; i++)
        {
            offset = SkipName(msg, offset);
            offset += 4;

            int ttl = (msg[offset] << 24) |
                      (msg[offset + 1] << 16) |
                      (msg[offset + 2] << 8) |
                      msg[offset + 3];

            offset += 4;

            int rd = (msg[offset] << 8) | msg[offset + 1];
            offset += 2 + rd;

            if (ttl < min)
                min = ttl;
        }

        return min == int.MaxValue ? -1 : min;
    }

    private static int SkipName(byte[] msg, int offset)
    {
        while (true)
        {
            byte len = msg[offset];

            if (len == 0)
                return offset + 1;

            if ((len & 0xC0) == 0xC0)
                return offset + 2;

            offset += len + 1;
        }
    }
}
