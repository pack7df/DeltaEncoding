using DeltaEncoding.RSync.Shared;
using DeltaEncoding.RSync.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DeltaEncoding.RSync.Server
{
    public class RsyncServerFileMetaPatchImpl : IRsyncServerFileMetaPatch
    {
        private PatchInfo patch;
        private Stream originalStream;
        public RsyncServerFileMetaPatchImpl(PatchInfo patch, Stream originalStream)
        {
            this.patch = patch;
            this.originalStream = originalStream;
        }

        public void GeneratePatch(Stream output)
        {
            output.Write(patch);
            foreach (var o in patch.Patchs)
            {
                if (o.Size == 0) continue;
                originalStream.Seek(o.Start, SeekOrigin.Begin);
                originalStream.Copy(output, o.Size);
            }
        }
    }
}
