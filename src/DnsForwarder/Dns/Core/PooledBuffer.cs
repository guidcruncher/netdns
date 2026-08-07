using System.Buffers;

namespace DnsForwarder.Dns.Core
{
    public sealed class PooledBuffer
    {
        public byte[] Buffer { get; }
        public int Length { get; }
        public bool FromPool { get; }

        public PooledBuffer(byte[] buffer, int length, bool fromPool)
        {
            Buffer = buffer;
            Length = length;
            FromPool = fromPool;
        }

        public void Return()
        {
            if (FromPool && Buffer != null)
            {
                ArrayPool<byte>.Shared.Return(Buffer, clearArray: true);
            }
        }
    }
}
