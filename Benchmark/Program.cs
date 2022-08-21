// See https://aka.ms/new-console-template for more information

using DeltaEncoding.RSync.Client;
using DeltaEncoding.RSync.Server;
using System.Diagnostics;

double RunWithStatistics(Action action, string processTitle = "Proceso")
{
    var start = DateTime.Now;
    action();
    var end = DateTime.Now;
    var dif = (end - start).TotalMinutes;
    Debug.WriteLine($"{processTitle} : {dif}");
    return dif;
}

var clientVersion = new FileStream("c:/1.zip", FileMode.Open);
//Server file version. 
var serverVersion = new FileStream("c:/2.zip", FileMode.Open);

var l = (ushort)2048;
Debug.WriteLine($"Starting process with BlockSize = {l}, client file size: {clientVersion.Length}, server file size: {serverVersion.Length}");
var client = new RSyncClientImplementation(l);
var server = new RSyncServerImplementation();
//var q = 733;
//var l = (int)Math.Sqrt(((double)clientVersion.Length/(double)q)*28.0d);

//Client signatures to send to server.
var signaturesStream = new MemoryStream();
//Patch stream from server to client.
var patchStream = new MemoryStream();
//Result stream in client to overwrite.
var resultStream = new FileStream("d:/3.zip", FileMode.Create);

//Generate signatures and create a client file receiver.
IRsyncClientFileUpdater fileReceiver = null;
var signaturesGeneration = RunWithStatistics(() => {
    fileReceiver = client.GetSignatures(clientVersion, signaturesStream);
}, "Generating signatures");

//Reset signatures stream to be used in server.
signaturesStream.Seek(0, SeekOrigin.Begin);

IRsyncServerFileSignatured serverFileSignatures = null;
//Read signatures in server.
var signaturesPreparation = RunWithStatistics(() => {
    serverFileSignatures = server.ReadClientFileSignature(signaturesStream);
}, "Preparing signatures");
//Generate meta patch information in server and Patch information.
IRsyncServerFileMetaPatch metaPach = null;
var metaPatchGeneration = RunWithStatistics(() => {
    metaPach = serverFileSignatures.GenerateMetaPatch(serverVersion);
}, "Generating meta patches");

var patchGeneration = RunWithStatistics(() => {
    metaPach.GeneratePatch(patchStream);
}, "Generating patches");

//Reset patch stream and client version to be used in client.
patchStream.Seek(0, SeekOrigin.Begin);
clientVersion.Seek(0, SeekOrigin.Begin);

bool equals = false;
//Update the client with patch stream.
var patchOperation = RunWithStatistics(() => {
    equals = fileReceiver.GetUpdate(patchStream, resultStream);
}, "Generating file update and check integrity");

var total = signaturesGeneration + signaturesPreparation + metaPatchGeneration + patchGeneration + patchOperation;
var clientTotal = signaturesGeneration + patchGeneration;
var serverTotal = signaturesPreparation + metaPatchGeneration + patchGeneration;
var s1 = ((double)signaturesGeneration / (double)clientTotal) * 100;
var s2 = ((double)patchOperation / (double)clientTotal) * 100;
var s3 = ((double)signaturesPreparation / (double)serverTotal) * 100;
var s4 = ((double)metaPatchGeneration / (double)serverTotal) * 100;
var s5 = ((double)patchGeneration / (double)serverTotal) * 100;
Debug.WriteLine($"S1:{signaturesGeneration} | {s1}%\n s2: {patchOperation} | {s2}%\n S3: {signaturesPreparation} | {s3}%\n s4: {metaPatchGeneration} | {s4}%\n s5: {patchGeneration} | {s5}%\n");