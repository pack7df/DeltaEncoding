using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DeltaEncoding.RSync.Shared
{
    public class PatchInfo
    {
        public ushort BlockSize
        {
            get; set;
        }

        public byte[] CheckSum
        {
            get;set;
        }

        public String StrongHashAlgorithmName
        {
            get;set;
        }

        public List<DeltaPatchInfo> Patchs
        {
            get; private set;
        } = new List<DeltaPatchInfo>();
    }
}
