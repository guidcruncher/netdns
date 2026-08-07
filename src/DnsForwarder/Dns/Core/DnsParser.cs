using System.Buffers.Binary;

namespace DnsForwarder.Dns.Core;

public static class DnsParser
{
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
        // Response code (last 4 bits of flags)
        msg.ResponseCode = ((flags & 0x000F)).ToString();

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

        // Ensure ResponseCode is set from header RCODE (last 4 bits)
        ushort headerFlags = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(2));
        msg.ResponseCode = ((headerFlags & 0x000F)).ToString();

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
        // Build NXDOMAIN response: copy header ID, set response + RCODE=3, echo question section.
        // Calculate question section size
        int qSize = 0;
        foreach (var q in request.Questions)
        {
            qSize += GetNameWireLength(q.Name) + 4; // qname + qtype(2) + qclass(2)
        }

        var buffer = new byte[12 + qSize];
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

        // Write question section
        foreach (var q in request.Questions)
        {
            offset += WriteNameWire(buffer, offset, q.Name);
            BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(offset), (ushort)q.Type);
            offset += 2;
            BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(offset), (ushort)q.Class);
            offset += 2;
        }

        return buffer;
    }

    private static int GetNameWireLength(string name)
    {
        if (string.IsNullOrEmpty(name))
            return 1; // root

        var parts = name.Split('.', StringSplitOptions.RemoveEmptyEntries);
        int len = 0;
        foreach (var p in parts)
            len += 1 + System.Text.Encoding.ASCII.GetByteCount(p);
        len += 1; // null terminator
        return len;
    }

    private static int WriteNameWire(byte[] buffer, int offset, string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            buffer[offset++] = 0;
            return 1;
        }

        var parts = name.Split('.', StringSplitOptions.RemoveEmptyEntries);
        foreach (var p in parts)
        {
            var bytes = System.Text.Encoding.ASCII.GetBytes(p);
            buffer[offset++] = (byte)bytes.Length;
            Array.Copy(bytes, 0, buffer, offset, bytes.Length);
            offset += bytes.Length;
        }

        buffer[offset++] = 0; // terminator
        return GetNameWireLength(name);
    }
}
