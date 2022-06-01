using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DeltaEncoding.RSync.Shared.Hash
{
    public class Addler32Hash : IRollingHash
    {
        private int start;
        private byte[] buffer;
        private int r1 = 0;
        private int r2 = 0;
        private ushort blockSize = 128;
        private const int m = 256 * 256;

        private void Start(ushort blockSize)
        {
            r1 = 0;
            r2 = 0;
            start = 0;
            this.blockSize = blockSize;
            buffer = new byte[2 * blockSize];
            for (var i = 0; i < buffer.Length; i++)
                buffer[i] = 0;

        }

        public Addler32Hash(ushort blockSize=128)
        {
            Start(blockSize);
        }

        public uint Push(byte b)
        {
            var current = buffer[start];
            r1 = (r1 - current + b) % m;
            if (r1 < 0) r1 += m;
            r2 = (r2 - blockSize * current + r1) % m;
            if (r2 < 0) r2 += m;
            buffer[start] = b;
            buffer[start + blockSize] = b;
            start++;
            if (start == blockSize) start = 0;
            return (UInt32)r2<<16 | (UInt32)r1;
        }

        public void SerializeConfig(Stream stream)
        {
            var writer = new BinaryWriter(stream);
            writer.Write(this.blockSize);
        }

        public void LoadConfig(Stream stream)
        {
            var reader = new BinaryReader(stream);
            var bs = reader.ReadUInt16();
            Start(bs);
        }

        public byte[] Content {
            get
            {
                var result = new byte[blockSize];
                Array.Copy(buffer, start, result, 0, blockSize);
                return result;
            }
        }

        public int Size
        {
            get
            {
                return blockSize;
            }
        }
    }
}
