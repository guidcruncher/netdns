using System.Net;
using System.Text;

namespace DnsForwarder.Dns.Core;

public sealed class DnsMessageReader
{
    private readonly byte[] _buffer;
    private int _offset;

    public DnsMessageReader(byte[] buffer)
    {
        _buffer = buffer;
        _offset = 0;
    }

    public DnsMessage Parse()
    {
        var msg = new DnsMessage();

        // Header
        msg.Id = ReadUInt16();
        ushort flags = ReadUInt16();
        msg.IsResponse = (flags & 0x8000) != 0;

        ushort qdCount = ReadUInt16();
        ushort anCount = ReadUInt16();
        _offset += 4; // NS + AR counts

        // Questions
        for (int i = 0; i < qdCount; i++)
        {
            var q = new DnsQuestion
            {
                Name = ReadName(),
                Type = ReadUInt16(),
                Class = ReadUInt16()
            };

            msg.Questions.Add(q);
        }

        // Answers
        for (int i = 0; i < anCount; i++)
        {
            var rr = new DnsResourceRecord
            {
                Name = ReadName(),
                Type = ReadUInt16(),
                Class = ReadUInt16(),
                Ttl = ReadInt32()
            };

            ushort rdLength = ReadUInt16();
            rr.RData = ReadBytes(rdLength);

            msg.Answers.Add(rr);

            // Extract first A/AAAA for metrics
            if (rr.Type == 1 && rdLength == 4) // A
                msg.AnswerAddress = new IPAddress(rr.RData);

            if (rr.Type == 28 && rdLength == 16) // AAAA
                msg.AnswerAddress = new IPAddress(rr.RData);
        }

        // Response code (last 4 bits of flags)
        msg.ResponseCode = ((flags & 0x000F)).ToString();

        return msg;
    }

    // ------------------------------------------------------------
    // DNS wire format helpers
    // ------------------------------------------------------------
    private ushort ReadUInt16()
    {
        ushort value = (ushort)((_buffer[_offset] << 8) | _buffer[_offset + 1]);
        _offset += 2;
        return value;
    }

    private int ReadInt32()
    {
        int value =
            (_buffer[_offset] << 24) |
            (_buffer[_offset + 1] << 16) |
            (_buffer[_offset + 2] << 8) |
            _buffer[_offset + 3];

        _offset += 4;
        return value;
    }

    private byte[] ReadBytes(int length)
    {
        var data = new byte[length];
        Buffer.BlockCopy(_buffer, _offset, data, 0, length);
        _offset += length;
        return data;
    }

    private string ReadName()
    {
        var sb = new StringBuilder();

        while (true)
        {
            byte len = _buffer[_offset++];

            if (len == 0)
                break;

            if ((len & 0xC0) == 0xC0)
            {
                // Compression pointer
                int pointer = ((_buffer[_offset - 1] & 0x3F) << 8) | _buffer[_offset];
                _offset++;
                sb.Append(ReadNameAt(pointer));
                break;
            }

            sb.Append(Encoding.ASCII.GetString(_buffer, _offset, len));
            _offset += len;

            sb.Append('.');
        }

        return sb.ToString().TrimEnd('.');
    }

    private string ReadNameAt(int pos)
    {
        var sb = new StringBuilder();

        while (true)
        {
            byte len = _buffer[pos++];

            if (len == 0)
                break;

            if ((len & 0xC0) == 0xC0)
            {
                int pointer = ((_buffer[pos - 1] & 0x3F) << 8) | _buffer[pos];
                pos++;
                sb.Append(ReadNameAt(pointer));
                break;
            }

            sb.Append(Encoding.ASCII.GetString(_buffer, pos, len));
            pos += len;

            sb.Append('.');
        }

        return sb.ToString().TrimEnd('.');
    }
}
