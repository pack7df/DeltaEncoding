using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DeltaEncoding.RSync.Shared.Serialization
{
    public static class SerializerHelper
    {

        public static IEnumerable<byte> GetBytes(this Stream stream)
        {
            var buffer = new byte[4 * 2014 * 1024];
            int readBytes;
            do
            {
                readBytes = stream.Read(buffer);
                for (var i = 0; i < readBytes; i++)
                    yield return buffer[i];
            }
            while (readBytes > 0);
        }

        public static void Write(this Stream stream, SignatureInfo signature)
        {
            var writer = new BinaryWriter(stream);
            writer.Write(signature.BlockSize);
            writer.Write(signature.StrongHashAlgorithmName);
            foreach (var s in signature.Chunks)
            {
                writer.Write(s.Weak);
                writer.Write(s.Strong);
            }
            writer.Flush();
        }

        public static void Write(this Stream deltaStream, PatchInfo delta)
        {
            var writer = new BinaryWriter(deltaStream);
            writer.Write(delta.BlockSize);
            writer.Write(delta.StrongHashAlgorithmName);
            writer.Write(delta.CheckSum);
            writer.Write(delta.Patchs.Count);
            foreach(var o in delta.Patchs)
            {
                writer.Write(o.BlockIndex);
                writer.Write(o.Size);
            }
        }

        public static void Copy(this Stream sourceStream, Stream targetStream, int size)
        {
            if (size <= 0) return;
            var buffer = new byte[4 * 1020 * 1024];
            while (size > 0)
            {
                var read = sourceStream.Read(buffer, 0, Math.Min(buffer.Length,size));
                size -= read;
                targetStream.Write(buffer, 0, read);
            }
        }
    }
}
