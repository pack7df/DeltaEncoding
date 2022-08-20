﻿using DeltaEncoding.RSync.Shared;
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

        public RSyncClientImplementation(ushort blockSize = 1024, String strongHashAlgorithmName = "MD5")
        {
            this.StrongHashAlgorithmName = strongHashAlgorithmName;
            this.BlockSize = blockSize;
        }

        public IRsyncClientFileUpdater GetSignatures(Stream original, Stream signatures)
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
            foreach(var b in original.GetBytes())
            {
                var weakSignature = weakHashCalculator.Push(b);
                offset = (offset+1)%BlockSize;
                if (offset != 0) continue;
                var strongSignature = strongHashCalculator.ComputeHash(weakHashCalculator.Content);
                chunks.Add(new BlockSignatureInfo(weakSignature, strongSignature));
            }
            signatures.Write(signature);
            return new RSyncClientFileUpdaterImpl(original);
        }
    }
}
