using DeltaEncoding.RSync.Shared;
using DeltaEncoding.RSync.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DeltaEncoding.RSync.Client
{
    public class RSyncClientFileUpdaterImpl : IRsyncClientFileUpdater
    {
        public Stream OriginalStream
        {
            get;set;
        }

        public RSyncClientFileUpdaterImpl(Stream originalStream)
        {
            this.OriginalStream = originalStream;
        }

        private bool CheckChecksum(Stream output, PatchInfo info)
        {
            var strongHashAlgorithmName = info.StrongHashAlgorithmName;
            var algorithm = HashAlgorithm.Create(strongHashAlgorithmName);
            output.Seek(0, SeekOrigin.Begin);
            var md5 = algorithm.ComputeHash(output);
            return (md5.SequenceEqual(info.CheckSum));
        }

        private PatchInfo ReadPatchInfo(Stream patchStream)
        {
            var reader = new BinaryReader(patchStream);
            var blockSize = reader.ReadUInt16();
            var strongHashAlgorithmName = reader.ReadString();
            var algorithm = HashAlgorithm.Create(strongHashAlgorithmName);
            var checkSum = reader.ReadBytes(algorithm.HashSize / 8);
            var count = reader.ReadInt32();
            var result = new PatchInfo
            {
                BlockSize = blockSize,
                StrongHashAlgorithmName = strongHashAlgorithmName,
                CheckSum = checkSum
            };
            for (var i = 0; i < count; i++)
            {
                var deltaBlock = new DeltaPatchInfo
                {
                    BlockIndex = reader.ReadInt32(),
                    Size = reader.ReadInt32(),
                };
                result.Patchs.Add(deltaBlock);
            }
            return result;
        }

        public bool GetUpdate(Stream patch, Stream update)
        {
            var patchInfo = ReadPatchInfo(patch);
            foreach (var o in patchInfo.Patchs)
            {
                var blockIndex = o.BlockIndex;
                var size = o.Size;
                patch.Copy(update, size);
                if (o.BlockIndex < 0) continue;
                var position = blockIndex * patchInfo.BlockSize;
                OriginalStream.Seek(position, SeekOrigin.Begin);
                OriginalStream.Copy(update, patchInfo.BlockSize);
            }
            return CheckChecksum(update, patchInfo);
        }
    }
}
