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
    public class RSyncServerImplementation : IRSyncServer
    {
        private class ChunkInfo
        {
            public byte[] Strong;
            public int Index;
        }
        private ushort blockSize = 0;
        private HashAlgorithm strongHashAlgorithm;
        private IDictionary<uint, List<ChunkInfo>> chunks = new Dictionary<uint, List<ChunkInfo>>();
        private void InitializeChunksDictionary(Stream signatures)
        {
            var info = signatures.ReadSignatureInfo();
            var strongHashAlgorithmName = info.StrongHashAlgorithmName;
            strongHashAlgorithm = HashAlgorithm.Create(strongHashAlgorithmName);
            blockSize = info.BlockSize;
            this.delta = new PatchInfo
            {
                BlockSize = info.BlockSize,
                CheckSum = null,
                StrongHashAlgorithmName = strongHashAlgorithmName
            };
            for (var i=0; i<info.Chunks.Count; i++)
            {
                var index = i;
                var chunk = info.Chunks[i];
                var strong = chunk.Strong;
                var weak = chunk.Weak;
                var chunkInfo = new ChunkInfo
                {
                    Index = index,
                    Strong = strong
                };
                if (!chunks.TryGetValue(weak, out var current))
                {
                    current = new List<ChunkInfo>();
                    chunks[weak] = current;
                }
                current.Add(chunkInfo);
            }
        }

        private int getIndex(UInt32 weak, byte[] strong)
        {
            if (!chunks.TryGetValue(weak, out var chunkList)) return -1;
            var chunk = chunkList.FirstOrDefault(c => c.Strong.SequenceEqual(strong));
            if (chunk == null) return -1;
            return chunk.Index;
        }

        private int basePosition;
        private int slidePosition;
        private PatchInfo delta;
        private void ProcessEndFile()
        {
            var start = basePosition;
            var end = basePosition + slidePosition;
            if (end < start) return;
            var patch = new DeltaPatchInfo
            {
                BlockIndex = -1,
                Size = end - start,
                Start = basePosition
            };
            this.delta.Patchs.Add(patch);
        }

        private void FillChecksum(Stream targetStream)
        {
            var strongHashAlgorithmName = delta.StrongHashAlgorithmName;
            strongHashAlgorithm = HashAlgorithm.Create(strongHashAlgorithmName);
            var algorithm = MD5.Create();
            targetStream.Seek(0, SeekOrigin.Begin);
            var md5 = algorithm.ComputeHash(targetStream);
            delta.CheckSum = md5;
        }

        private void WriteNonMachedBlocks(Stream targetStream, Stream patchStream)
        {
            foreach (var o in delta.Patchs)
            {
                if (o.Size == 0) continue;
                targetStream.Seek(o.Start, SeekOrigin.Begin);
                targetStream.Copy(patchStream,o.Size);
            }
        }

        private void WriteBlocks2(Stream targetStream, Stream secondary)
        {
            foreach (var o in delta.Patchs)
            {
                if (o.Size == 0) continue;
                targetStream.Seek(o.Start, SeekOrigin.Begin);
                targetStream.Copy(secondary, o.Size);
            }
        }

        private void FillDeltaOperations(Stream targetStream)
        {
            var weakHashAlgorithm = new Addler32Hash(delta.BlockSize);
            basePosition = 0;
            var rollLoading = 0l;
            slidePosition = 0;
            foreach (var b in targetStream.GetBytes())
            {
                var weakSignature = weakHashAlgorithm.Push(b);
                slidePosition++;
                rollLoading++;
                if (rollLoading < blockSize)
                    continue;
                if (!chunks.ContainsKey(weakSignature))
                    continue;
                var strongSignature = strongHashAlgorithm.ComputeHash(weakHashAlgorithm.Content);
                var index = getIndex(weakSignature, strongSignature);
                if (index==-1)
                    continue;
                var patch = new DeltaPatchInfo
                {
                    BlockIndex = index,
                    Start = basePosition,
                    Size = slidePosition - blockSize,
                };
                rollLoading = 0;
                basePosition += slidePosition;
                slidePosition = 0;
                this.delta.Patchs.Add(patch);
            }
            ProcessEndFile();
        }

        public void CreatePatch(Stream signaturesStream, Stream targetStream, Stream patchStream)
        {
            InitializeChunksDictionary(signaturesStream);
            FillDeltaOperations(targetStream);
            FillChecksum(targetStream);
            patchStream.Write(delta);
            WriteNonMachedBlocks(targetStream,patchStream);
        }


    }
}
