using DeltaEncoding.RSync.Shared;
using DeltaEncoding.RSync.Shared.Hash;
using DeltaEncoding.RSync.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DeltaEncoding.RSync.Client
{
    public class RSyncClientImplementation : IRSyncClient
    {
        public ushort BlockSize
        {
            get;set;
        }

        public String StrongHashAlgorithmName
        {
            get;set;
        }

        public long MaxSignatureSize
        {
            get; set;
        } = 6 * 1024l * 1024l * 1024l;

        public RSyncClientImplementation(ushort blockSize = 1024, String strongHashAlgorithmName = "MD5")
        {
            this.StrongHashAlgorithmName = strongHashAlgorithmName;
            this.BlockSize = blockSize;
        }

        public void CreateSignatures(Stream input, Stream output)
        {
            var weakHashCalculator = new Addler32Hash(this.BlockSize);
            var strongHashCalculator = HashAlgorithm.Create(this.StrongHashAlgorithmName);
            var signature = new SignatureInfo
            {
                BlockSize = this.BlockSize,
                StrongHashAlgorithmName = this.StrongHashAlgorithmName,
            };
            var chunks = signature.Chunks;
            int offset = 0;
            foreach(var b in input.GetBytes())
            {
                var weakSignature = weakHashCalculator.Push(b);
                offset++;
                if (offset == BlockSize)
                    offset = 0;
                if (offset != 0) continue;
                var strongSignature = strongHashCalculator.ComputeHash(weakHashCalculator.Content);
                chunks.Add(new BlockSignatureInfo(weakSignature, strongSignature));
            }
            output.Write(signature);
        }

        private bool CheckChecksum(Stream output, PatchInfo info)
        {
            var strongHashAlgorithmName = info.StrongHashAlgorithmName;
            var algorithm = HashAlgorithm.Create(strongHashAlgorithmName);
            output.Seek(0, SeekOrigin.Begin);
            var md5 = algorithm.ComputeHash(output);
            return (md5.SequenceEqual(info.CheckSum));
        }

        public bool Patch(Stream patchStream, Stream originalStream, Stream outputStream)
        {
            var patchInfo = patchStream.ReadPatchInfo();
            foreach(var o in patchInfo.Patchs)
            {
                var blockIndex = o.BlockIndex;
                var size = o.Size;
                patchStream.Copy(outputStream, size);
                if (o.BlockIndex < 0) continue;
                var position = blockIndex * patchInfo.BlockSize;
                originalStream.Seek(position, SeekOrigin.Begin);
                originalStream.Copy(outputStream, patchInfo.BlockSize);
            }
            return CheckChecksum(outputStream, patchInfo);
        }
    }
}
