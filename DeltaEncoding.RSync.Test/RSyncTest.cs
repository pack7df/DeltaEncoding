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
            //Client file version to update.
            var clientVersion = new FileStream("./samples/d1.dll", FileMode.Open);
            //Server file version. 
            var serverVersion = new FileStream("./samples/d2.dll", FileMode.Open);

            var client = new RSyncClientImplementation();
            var server = new RSyncServerImplementation();
            //var q = 733;
            //var l = (int)Math.Sqrt(((double)stream1.Length/(double)q)*28.0d);
            //var l= 2048;
            //Client signatures to send to server.
            var signaturesStream = new MemoryStream();
            //Patch stream from server to client.
            var patchStream = new MemoryStream();
            //Result stream in client to overwrite.
            var resultStream = new MemoryStream();

            //Generate signatures and create a client file receiver.
            var fileReceiver = client.GetSignatures(clientVersion, signaturesStream);

            //Reset signatures stream to be used in server.
            signaturesStream.Seek(0, SeekOrigin.Begin);

            //Read signatures in server.
            var serverFileSignatures = server.ReadClientFileSignature(signaturesStream);
            //Generate meta patch information in server and Patch information.
            serverFileSignatures.GenerateMetaPatch(serverVersion).GeneratePatch(patchStream);
            //Reset patch stream and client version to be used in client.
            patchStream.Seek(0, SeekOrigin.Begin);
            clientVersion.Seek(0, SeekOrigin.Begin);

            //Update the client with patch stream.
            var equals = fileReceiver.GetUpdate(patchStream, resultStream);
            Assert.True(equals);
        }
    }
}
