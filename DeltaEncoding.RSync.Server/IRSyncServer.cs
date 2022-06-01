using System.IO;

namespace DeltaEncoding.RSync.Server
{
    public interface IRSyncServer
    {
        void CreatePatch(Stream signatures, Stream targetStream, Stream delta);
    }
}
