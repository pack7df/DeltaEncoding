using System.IO;

namespace DeltaEncoding.RSync.Client
{
    public interface IRsyncClientFileUpdater
    {
        bool GetUpdate(Stream patch, Stream update);
    }

    public interface IRSyncClient
    {
        IRsyncClientFileUpdater GetSignatures(Stream original, Stream signatures);
    }
}
