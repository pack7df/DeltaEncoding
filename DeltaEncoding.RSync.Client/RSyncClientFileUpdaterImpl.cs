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

        public ushort BlockSize
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
            var algorithm = HashAlgorithm.Create("MD5");
            output.Seek(0, SeekOrigin.Begin);
            var md5 = algorithm.ComputeHash(output);
            return (md5.SequenceEqual(info.CheckSum));
        }

        private PatchInfo ReadPatchInfo(Stream patchStream)
        {
            var md5 = HashAlgorithm.Create("MD5");
            var reader = new BinaryReader(patchStream);
            BlockSize = reader.ReadUInt16();
            var strongHashAlgorithmName = reader.ReadString();
            var algorithm = HashAlgorithm.Create(strongHashAlgorithmName);
            var checkSum = reader.ReadBytes(md5.HashSize / 8);
            var count = reader.ReadInt32();
            var result = new PatchInfo
            {
                BlockSize = BlockSize,
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
            var buffer = new byte[4*1024*1024]; 
            foreach (var o in patchInfo.Patchs)
            {
                var blockIndex = o.BlockIndex;
                var size = o.Size;
                while (size > 0)
                {
                    var read = patch.Read(buffer, 0, Math.Min(buffer.Length, size));
                    size -= read;
                    update.Write(buffer, 0, read);
                }
                if (o.BlockIndex < 0) continue;
                var position = blockIndex * patchInfo.BlockSize;
                if (OriginalStream.Position!=position)
                    OriginalStream.Seek(position, SeekOrigin.Begin);
                size = BlockSize;
                while (size > 0)
                {
                    var read = OriginalStream.Read(buffer, 0, Math.Min(buffer.Length, size));
                    size -= read;
                    update.Write(buffer, 0, read);
                }
            }
            return CheckChecksum(update, patchInfo);
        }
    }
}
