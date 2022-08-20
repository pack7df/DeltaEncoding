using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaEncoding.RSync.Shared.ArithmeticEncoding
{
    public class ArithmeticCompressor : IArithmeticEncoder
    {
        private ulong start;
        private ulong end;
        private static ulong MAX = ulong.MaxValue;
        private static ulong MIN = ulong.MinValue;
        private static ulong HALF = (MAX+MIN)>>1;
        private IDictionary<byte, ulong> s = new Dictionary<byte,ulong>();
        private IDictionary<byte, ulong> f = new Dictionary<byte, ulong>();
        byte[] buffer = new byte[4 * 1024 * 1024];

        private void sendBitBuffer(byte bitValue)
        {

        }

        public void Push(byte[] bytes, int start, int length)
        {
            for (var i=start; i<length+start; i++)
            {
                var d = bytes[i];
                var newStart = s[d];
                var newEnd = s[d] + f[d];
                if (newStart > HALF)
                    sendBitBuffer(1);
                if (newEnd <= HALF)
                    sendBitBuffer(0);
            }
        }
    }
}
