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

        private SignatureInfo ReadSignatureInfo(Stream stream)
        {
            using var reader = new BinaryReader(stream);
            var blockSize = reader.ReadUInt16();
            var strongHashAlgorithmName = reader.ReadString();
            var result = new SignatureInfo
            {
                BlockSize = blockSize,
                StrongHashAlgorithmName = strongHashAlgorithmName,
            };
            var algorithm = HashAlgorithm.Create(result.StrongHashAlgorithmName);
            var size = stream.Length;
            while (reader.BaseStream.Position < size)
            {
                var weak = reader.ReadUInt32();
                var strong = reader.ReadBytes(algorithm.HashSize / 8);
                var chunck = new BlockSignatureInfo(weak, strong);
                result.Chunks.Add(chunck);
            }
            return result;
        }
        private void InitializeChunksDictionary(Stream signatures)
        {
            var info = ReadSignatureInfo(signatures);
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

        private void WriteNonMachedBlocks(Stream targetStream, Stream temporalStream)
        {
            foreach (var o in delta.Patchs)
            {
                if (o.Size == 0) continue;
                targetStream.Seek(o.Start, SeekOrigin.Begin);
                targetStream.Copy(temporalStream,o.Size);
            }
        }

        private void FillDeltaOperations(Stream targetStream)
        {
            var weakHashAlgorithm = new Addler32Hash(delta.BlockSize);
            var md5 = HashAlgorithm.Create("MD5");
            md5.Initialize();
            basePosition = 0;
            var rollLoading = 0l;
            slidePosition = 0;
            var md5Buffer = new byte[1024];
            var md5Load = 0;
            foreach (var b in targetStream.GetBytes())
            {
                md5Buffer[md5Load] = b;
                md5Load++;
                if (md5Load==md5Buffer.Length)
                {
                    md5Load = 0;
                    md5.TransformBlock(md5Buffer, 0, md5Buffer.Length, md5Buffer, 0);
                }
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
            md5.TransformFinalBlock(md5Buffer, 0,md5Load);
            delta.CheckSum = md5.Hash;
            ProcessEndFile();
        }

        private void CompressPatch(Stream temporalStream, Stream patchStream)
        {
            var frecuencies = new Dictionary<byte, long>();
            temporalStream.Seek(0, SeekOrigin.Begin);
            foreach (var b in temporalStream.GetBytes())
            {
                if (!frecuencies.TryGetValue(b, out var frec))
                    frec = 0;
                frec++;
                frecuencies[b] = frec;
            }
            foreach (var b in temporalStream.GetBytes())
            {

            }
        }

        public void CreatePatch(Stream signaturesStream, Stream targetStream, Stream deltaStream, Stream compressStream = null)
        {
            InitializeChunksDictionary(signaturesStream);
            FillDeltaOperations(targetStream);
            var temporalStream = compressStream;
            if (compressStream == null)
                temporalStream = deltaStream;
            temporalStream.Write(delta);
            WriteNonMachedBlocks(targetStream, temporalStream);
            if (compressStream == null) return;
            CompressPatch(temporalStream, deltaStream);
        }
    }
}
