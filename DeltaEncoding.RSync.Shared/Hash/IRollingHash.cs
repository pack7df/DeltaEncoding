using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
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
        public byte[] GetHash(HashAlgorithm algoritm);
        public void SerializeConfig(Stream stream);
        public void LoadConfig(Stream stream);

        public UInt32 GetWeakCode(byte[] bytes);
    }
}
