using SyncAppVeeam.Models;

namespace SyncAppVeeam.Classes
{
    public sealed class SyncManagerService : IDisposable
    {
        private readonly FolderNode _source;
        private readonly FolderNode _sync;
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
            _source.IsSynced = CompareNode(this._source, this._sync);
            PrintFileTree();
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

        //Big block of code incoming
        #region Recursive tree processing
        // Starts with comparing the two nodes - the root ones.
        public bool CompareNode(IEntry source, IEntry? replica)
        {
            // If any way that a mirror entry does not exist - marks source as out of sync and checks if its a folder to mark all other nodes inside it as out of sync
            if (replica == null)
            {
                source.IsSynced = false;
                if (source is FolderNode sourcefolder)
                {
                    foreach (var node in sourcefolder.content)
                    {
                        node.IsSynced = CompareNode(node, null);
                    }
                }
                // If anything turns null it means its not in sync - therefore false
                return false;
            }
            else
            {
                // Check if the nodes are the same type
                if (source.GetType() != replica.GetType()) return false;

                // If nodes are files, compare them
                if (source is FileNode file && replica is FileNode fileSync)
                {
                    return CheckFile(file, fileSync);
                }

                // If nodes are folders - send them to CheckFolder
                if (source is FolderNode folder && replica is FolderNode folderSync)
                {
                    return CheckFolder(folder, folderSync);
                }

                // If their names are wrong - false, this needs to be last to keep checking other nodes in the tree.
                if (source.Name != replica.Name) return false;
            }
            return true;
        }
        public bool CheckFolder(FolderNode source, FolderNode tosync)
        {
            // Start by iterating over all nodes in the folder
            foreach (var node in source.content)
            {
                // Try to find a match by name
                var match = tosync.content.SingleOrDefault<IEntry>(x => x.Name == node.Name);

                // If a match is found - compare the two again
                if (match != null)
                {
                    node.IsSynced = CompareNode(node, match);
                }
                // If none is found - try to compare them with null match to flag every node under it (if its a folder) as out of sync
                else
                {
                    node.IsSynced = false;
                    if (node is FolderNode)
                    {
                        node.IsSynced = CompareNode(node, match);
                    }
                }
            }
            // Final result of folder comparison is determined by all other nodes being synced.
            return source.content.All(a => a.IsSynced);
        }

        public bool CheckFile(FileNode source, FileNode sync)
        {
            // Check if files have the same path(and name), and their timestamp, if any doesnt match returns false => needs correction
            if (source.Name != sync.Name) return false;
            // Check if source is newer - could also be != for checking if the timestamp matches overall
            if (source.modified > sync.modified) return false;
            return true;
        }
        #endregion

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
