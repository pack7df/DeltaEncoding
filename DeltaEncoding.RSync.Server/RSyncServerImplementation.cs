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
        public IRsyncServerFileSignatured ReadClientFileSignature(Stream signatures)
        {
            var chunks = new Dictionary<uint, List<ChunkInfo>>();
            var info = ReadSignatureInfo(signatures);
            var strongHashAlgorithmName = info.StrongHashAlgorithmName;
            var patch = new PatchInfo
            {
                BlockSize = info.BlockSize,
                CheckSum = null,
                StrongHashAlgorithmName = info.StrongHashAlgorithmName
            };
            for (var i = 0; i < info.Chunks.Count; i++)
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
            return new RsyncServerFileSignaturedImpl(patch, chunks);
        }
    }
}
