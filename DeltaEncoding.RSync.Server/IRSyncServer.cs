using System.IO;

namespace DeltaEncoding.RSync.Server
{


    public interface IRsyncServerFileMetaPatch
    {
        void GeneratePatch(Stream output);
    }

    public interface IRsyncServerFileSignatured
    {
        IRsyncServerFileMetaPatch GenerateMetaPatch(Stream originalStream);
    }

    

    public interface IRSyncServer
    {
        IRsyncServerFileSignatured ReadClientFileSignature(Stream signatures);
    }
}
