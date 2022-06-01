using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DeltaEncoding.RSync.Shared
{
    public class DeltaPatchInfo
    {
        public long Start
        {
            get; set;
        } = 0;
        public int Size
        {
            get; set;
        } = 0;
        public int BlockIndex
        {
            get;set;
        }
    }
}
