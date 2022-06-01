using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DeltaEncoding.RSync.Shared.Hash
{
    public interface IRollingHash
    {
        public int Size
        {
            get;
        }
        public UInt32 Push(byte b);
        public byte[] Content
        {
            get;
        }
        public void SerializeConfig(Stream stream);
        public void LoadConfig(Stream stream);
    }
}
