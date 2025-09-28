using SyncAppVeeam.Models;

namespace SyncAppVeeam.Classes
{
    public sealed class SyncManagerService : IDisposable
    {
        private readonly FolderNode _source;
        private readonly FolderNode _sync;
        private List<IEntry> _unsynced = new();
        private readonly TimeSpan _syncInterval;
        private string _destinationRoot;
        private string _sourceRoot;

        public SyncManagerService(string source, string sync, TimeSpan interval)
        {
            this._destinationRoot = sync;
            this._sourceRoot = source;
            this._source = GetFileTree(source);
            this._sync = GetFileTree(sync);
            this._syncInterval = interval;
            if (interval == TimeSpan.Zero)
            {
                // Sync Once and immediatelly, then end
                // Implementation
            }
            _source.IsSynced = CompareNode(this._source, this._sync);
            PrintFileTree();
            Synchronize();
        }

        public void Synchronize()
        {
            using (var syncService = new SynchronizationService(_unsynced, _destinationRoot, _sourceRoot))
            {
                syncService.RunSync();
            }
        }

        public FolderNode GetFileTree(string path)
        {
            FolderNode? result = null;
            //Check if provided path is a dir
            if (Directory.Exists(path))
            {
                result = new FolderNode(Path.GetFileName(path), path, "\\", true);
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

        // Big block of code incoming
        #region Recursive tree processing
        // start by comparing two roots
        public bool CompareNode(IEntry source, IEntry? replica)
        {
            if (replica == null)
            {
                source.IsSynced = false;
                _unsynced.Add(source);

                // replica is null? process further to flag all as false
                if (source is FolderNode sourcefolder)
                {
                    foreach (var node in sourcefolder.content)
                    {
                        node.IsSynced = CompareNode(node, null);
                    }
                }
                // anything null? then always false
                return false;
            }
            else
            {
                // same type?
                if (source.GetType() != replica.GetType())
                {
                    _unsynced.Add(source);
                    return false;
                }
                ;

                // files? - compare
                if (source is FileNode file && replica is FileNode fileSync)
                {
                    bool result = CheckFile(file, fileSync);
                    source.IsSynced = result;
                    if (!result) _unsynced.Add(source);
                    return result;
                }

                // folders? - compare content
                if (source is FolderNode folder && replica is FolderNode folderSync)
                {
                    bool result = CheckFolder(folder, folderSync);
                    source.IsSynced = result;
                    if (!result) _unsynced.Add(source);
                    return result;
                }

                // same name?
                if (source.Name != replica.Name)
                {
                    source.IsSynced = false;
                    _unsynced.Add(source);
                    return false;
                }
            }
            return true;
        }
        public bool CheckFolder(FolderNode source, FolderNode tosync)
        {
            // Start by iterating over all nodes in the folder
            foreach (var node in source.content)
            {
                // find a match by name
                var match = tosync.content.SingleOrDefault<IEntry>(x => x.Name == node.Name);

                // if match then compare
                if (match != null)
                {
                    node.IsSynced = CompareNode(node, match);
                }
                // if no match - false, unless its a folder, then keep searching
                else
                {
                    node.IsSynced = false;
                    _unsynced.Add(node);
                    if (node is FolderNode)
                    {
                        node.IsSynced = CompareNode(node, match);
                    }
                }
            }
            // final result of a folder comparison is determined by all its nodes being synced or not
            return source.content.All(a => a.IsSynced);
        }

        public bool CheckFile(FileNode source, FileNode sync)
        {
            // same name, or timestamp match?
            if (source.Name != sync.Name || source.modified > sync.modified)
            {
                _unsynced.Add(source);
                return false;
            }
            return true;
        }

        private void FlagNode(IEntry node)
        {
            node.IsSynced = false;
            _unsynced.Add(node);
        }
        #endregion

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
