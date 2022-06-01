using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DeltaEncoding.RSync.Shared
{
    public class SignatureInfo
    {
        public ushort BlockSize {
            get; set;
        }
        public String StrongHashAlgorithmName
        {
            get;set;
        }
        public List<BlockSignatureInfo> Chunks
        {
            get;
            private set;
        } = new List<BlockSignatureInfo>();
    }
}
