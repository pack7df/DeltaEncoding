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
            if (end <= start) return;
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
            slidePosition = 0;
            var buffer = new byte[patch.BlockSize];
            while (true)
            {
                var read = originalStream.Read(buffer, 0, buffer.Length);
                slidePosition+= read;
                if (read<buffer.Length)
                    break;
                var weakSignature = weakHashAlgorithm.GetWeakCode(buffer);
                while (true)
                {
                    if (chunks.ContainsKey(weakSignature))
                    {
                        var strongSignature = weakHashAlgorithm.GetHash(algorithm);
                        var index = getIndex(weakSignature, strongSignature);
                        if (index != -1)
                        {
                            var deltaPatch = new DeltaPatchInfo
                            {
                                BlockIndex = index,
                                Start = basePosition,
                                Size = slidePosition - patch.BlockSize,
                            };
                            basePosition += slidePosition;
                            slidePosition = 0;
                            this.patch.Patchs.Add(deltaPatch);
                            break;
                        }
                    }
                    var b = originalStream.ReadByte();
                    if (b == -1)
                        break;
                    weakSignature = weakHashAlgorithm.Push((byte)b);
                    slidePosition++;
                }
            }
            ProcessEndFile();
            originalStream.Seek(0, SeekOrigin.Begin);
            var hash = md5.ComputeHash(originalStream);
            patch.CheckSum = hash;
            return this;
        }

        public void GeneratePatch(Stream output)
        {
            output.Write(patch);
            var buffer = new byte[4 * 1020 * 1024];
            foreach (var o in patch.Patchs)
            {
                if (o.Size == 0) continue;
                originalStream.Seek(o.Start, SeekOrigin.Begin);
                var size = o.Size;
                while (size > 0)
                {
                    var read = originalStream.Read(buffer, 0, Math.Min(buffer.Length, size));
                    size -= read;
                    output.Write(buffer, 0, read);
                }
            }
        }
    }
}
