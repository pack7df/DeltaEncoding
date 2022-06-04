using DeltaEncoding.RSync.Client;
using DeltaEncoding.RSync.Server;
using System;
using System.IO;
using Xunit;

namespace DeltaEncoding.RSync.Test
{
    public class RSyncTest
    {
        [Fact]
        public void Test1()
        {
            var stream1 = new FileStream("./samples/d1.dll", FileMode.Open);
            var stream2 = new FileStream("./samples/d2.dll", FileMode.Open);
            var client = new RSyncClientImplementation();
            var server = new RSyncServerImplementation();
            var q = 733;
            //var l = (int)Math.Sqrt(((double)stream1.Length/(double)q)*28.0d);
            var l= 2048;
            var signaturesStream = new MemoryStream();
            var patchStream = new MemoryStream();
            var resultStream = new MemoryStream();
            client.CreateSignatures(stream1, signaturesStream);
            signaturesStream.Seek(0, SeekOrigin.Begin);
            server.CreatePatch(signaturesStream, stream2, patchStream);
            patchStream.Seek(0, SeekOrigin.Begin);
            stream1.Seek(0, SeekOrigin.Begin);
            var equals = client.Patch(patchStream, stream1, resultStream);
            Assert.True(equals);
        }
    }
}
