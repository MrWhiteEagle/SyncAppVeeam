using SyncAppVeeam.Models;

namespace SyncAppVeeam.Classes
{
    public sealed class SyncManagerService : IDisposable
    {
        private readonly IEntry _source;
        private readonly IEntry _sync;
        private readonly TimeSpan _syncInterval;

        public SyncManagerService(string source, string sync, TimeSpan interval)
        {
            this._source = GetFileTree(source);
            this._sync = GetFileTree(sync);
            this._syncInterval = interval;
            if (interval == TimeSpan.Zero)
            {
                // Sync Once and immediatelly, then end
                // Implementation
            }
            PrintFileTree();
        }

        public IEntry GetFileTree(string path)
        {
            FolderNode? result = null;
            //Check if provided path is a dir
            if (Directory.Exists(path))
            {
                result = new FolderNode(Path.GetFileName(path), path);
            }
            return result ?? throw new InvalidOperationException($"Provided path: {path} does not exist or is not a directory");
        }

        public void PrintFileTree()
        {
            Console.WriteLine("========================");
            Console.WriteLine("Source Tree:");
            Console.WriteLine("========================");
            _source.PrintContent();
            Console.WriteLine("========================");
            Console.WriteLine("Sync Tree:");
            _sync.PrintContent();
            Console.WriteLine("========================");
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
