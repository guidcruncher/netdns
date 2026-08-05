using System.Buffers.Binary;

namespace DnsForwarder;

public static class DnsParser
{
    // NOTE: This is a simplified parser; for production use a mature DNS library.
    public static DnsMessage Parse(byte[] buffer)
    {
        var msg = new DnsMessage();

        if (buffer.Length < 12)
            throw new InvalidOperationException("DNS message too short");

        int offset = 0;

        msg.Id = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset));
        offset += 2;

        ushort flags = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset));
        offset += 2;

        msg.IsResponse = (flags & 0x8000) != 0;

        ushort qdCount = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset));
        offset += 2;
        ushort anCount = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset));
        offset += 2;
        ushort nsCount = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset));
        offset += 2;
        ushort arCount = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset));
        offset += 2;

        for (int i = 0; i < qdCount; i++)
        {
            var (name, newOffset) = ReadName(buffer, offset);
            offset = newOffset;

            ushort type = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset));
            offset += 2;
            ushort cls = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset));
            offset += 2;

            msg.Questions.Add(new DnsQuestion
            {
                Name = name,
                Type = type,
                Class = cls
            });
        }

        for (int i = 0; i < anCount; i++)
        {
            var (name, newOffset) = ReadName(buffer, offset);
            offset = newOffset;

            ushort type = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset));
            offset += 2;
            ushort cls = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset));
            offset += 2;
            int ttl = BinaryPrimitives.ReadInt32BigEndian(buffer.AsSpan(offset));
            offset += 4;
            ushort rdLength = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset));
            offset += 2;

            if (offset + rdLength > buffer.Length)
                throw new InvalidOperationException("RDATA length exceeds buffer");

            var rdata = buffer.Skip(offset).Take(rdLength).ToArray();
            offset += rdLength;

            msg.Answers.Add(new DnsResourceRecord
            {
                Name = name,
                Type = type,
                Class = cls,
                Ttl = ttl,
                RData = rdata
            });
        }

        return msg;
    }

    private static (string name, int offset) ReadName(byte[] buffer, int offset)
    {
        var labels = new List<string>();
        int originalOffset = offset;
        bool jumped = false;
        int jumpOffset = -1;

        while (true)
        {
            if (offset >= buffer.Length)
                throw new InvalidOperationException("Name exceeds buffer");

            byte len = buffer[offset++];

            if (len == 0)
                break;

            if ((len & 0xC0) == 0xC0)
            {
                if (offset >= buffer.Length)
                    throw new InvalidOperationException("Pointer exceeds buffer");

                byte b2 = buffer[offset++];
                int pointer = ((len & 0x3F) << 8) | b2;

                if (!jumped)
                {
                    jumpOffset = offset;
                    jumped = true;
                }

                offset = pointer;
                continue;
            }

            if (offset + len > buffer.Length)
                throw new InvalidOperationException("Label exceeds buffer");

            var label = System.Text.Encoding.ASCII.GetString(buffer, offset, len);
            offset += len;
            labels.Add(label);
        }

        if (jumped && jumpOffset != -1)
            offset = jumpOffset;

        var name = string.Join(".", labels);
        return (name, offset);
    }

    public static byte[] BuildBlockedResponse(DnsMessage request)
    {
        // Very simple NXDOMAIN response: copy header ID, set response + RCODE=3, no answers.
        var buffer = new byte[12];
        int offset = 0;

        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(offset), request.Id);
        offset += 2;

        ushort flags = 0;
        flags |= 0x8000; // QR = 1 (response)
        flags |= 0x0003; // RCODE = 3 (NXDOMAIN)
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(offset), flags);
        offset += 2;

        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(offset), (ushort)request.Questions.Count);
        offset += 2;
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(offset), 0); // ANCOUNT
        offset += 2;
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(offset), 0); // NSCOUNT
        offset += 2;
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(offset), 0); // ARCOUNT
        offset += 2;

        // For simplicity, we don't echo the question section here.
        // A more complete implementation should copy the original question bytes.
        return buffer;
    }
}
