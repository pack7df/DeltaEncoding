using System;

namespace DeltaEncoding.RSync
{
    public class BlockSignatureInfo
    {
        public BlockSignatureInfo(UInt32 weak, byte[] strong)
        {
            this.Strong = strong;
            this.Weak = weak;
        }

        public UInt32 Weak
        {
            get; private set;
        }
        public byte[] Strong
        {
            get;private set;
        }


    }
}
