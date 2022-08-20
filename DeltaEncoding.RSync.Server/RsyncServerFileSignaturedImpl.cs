using DeltaEncoding.RSync.Shared;
using DeltaEncoding.RSync.Shared.Hash;
using DeltaEncoding.RSync.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DeltaEncoding.RSync.Server
{
    public class RsyncServerFileSignaturedImpl : IRsyncServerFileSignatured, IRsyncServerFileMetaPatch
    {
        private PatchInfo patch;
        private IDictionary<uint, List<ChunkInfo>> chunks;
        private int basePosition;
        private int slidePosition;
        private Stream originalStream;
        

        public RsyncServerFileSignaturedImpl(PatchInfo patch, IDictionary<uint, List<ChunkInfo>> chunks)
        {
            this.patch = patch;
            this.chunks = chunks;
        }

        private void ProcessEndFile()
        {
            var start = basePosition;
            var end = basePosition + slidePosition;
            if (end < start) return;
            var delta = new DeltaPatchInfo
            {
                BlockIndex = -1,
                Size = end - start,
                Start = basePosition
            };
            this.patch.Patchs.Add(delta);
        }

        private int getIndex(UInt32 weak, byte[] strong)
        {
            if (!chunks.TryGetValue(weak, out var chunkList)) return -1;
            var chunk = chunkList.FirstOrDefault(c => c.Strong.SequenceEqual(strong));
            if (chunk == null) return -1;
            return chunk.Index;
        }

        public IRsyncServerFileMetaPatch GenerateMetaPatch(Stream originalStream)
        {
            this.originalStream = originalStream;
            var weakHashAlgorithm = new Addler32Hash(patch.BlockSize);
            var md5 = HashAlgorithm.Create("MD5");
            var algorithm = HashAlgorithm.Create(this.patch.StrongHashAlgorithmName);
            md5.Initialize();
            basePosition = 0;
            var rollLoading = 0l;
            slidePosition = 0;
            var md5Buffer = new byte[1024];
            var md5Load = 0;
            foreach (var b in originalStream.GetBytes())
            {
                md5Buffer[md5Load] = b;
                md5Load++;
                if (md5Load == md5Buffer.Length)
                {
                    md5Load = 0;
                    md5.TransformBlock(md5Buffer, 0, md5Buffer.Length, md5Buffer, 0);
                }
                var weakSignature = weakHashAlgorithm.Push(b);
                slidePosition++;
                rollLoading++;
                if (rollLoading < patch.BlockSize)
                    continue;
                if (!chunks.ContainsKey(weakSignature))
                    continue;
                var strongSignature = algorithm.ComputeHash(weakHashAlgorithm.Content);
                var index = getIndex(weakSignature, strongSignature);
                if (index == -1)
                    continue;
                var deltaPatch = new DeltaPatchInfo
                {
                    BlockIndex = index,
                    Start = basePosition,
                    Size = slidePosition - patch.BlockSize,
                };
                rollLoading = 0;
                basePosition += slidePosition;
                slidePosition = 0;
                this.patch.Patchs.Add(deltaPatch);
            }
            md5.TransformFinalBlock(md5Buffer, 0, md5Load);
            patch.CheckSum = md5.Hash;
            ProcessEndFile();
            return this;
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
