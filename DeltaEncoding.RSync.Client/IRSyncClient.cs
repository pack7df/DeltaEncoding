using System.IO;

namespace DeltaEncoding.RSync.Client
{
    public interface IRSyncClient
    {
        void CreateSignatures(Stream input, Stream output);
        bool Patch(Stream deltaStream, Stream originalStream, Stream outputStream);
    }
}
