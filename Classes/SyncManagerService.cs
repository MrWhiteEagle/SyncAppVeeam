using SyncAppVeeam.Models;

namespace SyncAppVeeam.Classes
{
    public sealed class SyncManagerService : IDisposable
    {
        private FolderNode _sourceNode;
        private FolderNode _destinationNode;
        private readonly List<INode> _unsynced = new();
        private readonly TimerService _timerService;
        private readonly string _sourceRoot;
        private readonly string _destinationRoot;

        public SyncManagerService(string source, string destination, TimeSpan interval)
        {
            this._sourceRoot = source;
            this._destinationRoot = destination;

            //Start the timer - creation = first sync
            this._timerService = new TimerService(interval);
            _timerService.TimeIsUp += Synchronize;
            _timerService.ForceTick();
        }

        private void Synchronize(object? sender, EventArgs e)
        {
            UserCLIService.CLIPrint($"Sync Requested at {DateTime.Now}");
            //Clear the list for next sweep
            _unsynced.Clear();

            //populate the nodes again
            this._sourceNode = GetFileTree(_sourceRoot);
            this._destinationNode = GetFileTree(_destinationRoot);

            // Had to keep it that way to later print out a whole tree marking unsynced nodes
            _sourceNode.IsSynced = CompareNode(_sourceNode, _destinationNode);
            PrintFileTree();
            var syncService = new SynchronizationService(_unsynced, _destinationNode.NodePath, _sourceNode.NodePath);
            syncService.RunSync();
        }

        private FolderNode GetFileTree(string path)
        {
            FolderNode? result = null;
            //Check if provided path is a dir
            if (Directory.Exists(path))
            {
                result = new FolderNode(Path.GetFileName(path), path, "\\", true);
            }
            return result ?? throw new InvalidOperationException($"Provided path: {path} does not exist or is not a directory");
        }

        private void PrintFileTree()
        {
            UserCLIService.CLIPrint("========================");
            UserCLIService.CLIPrint("Source Tree:");
            UserCLIService.CLIPrint("========================");
            _sourceNode.PrintContent();
            UserCLIService.CLIPrint("========================");
            UserCLIService.CLIPrint("Sync Tree:");
            UserCLIService.CLIPrint("========================");
            _destinationNode.PrintContent();
        }

        // Big block of code incoming
        #region Recursive tree processing
        // start by comparing two roots
        private bool CompareNode(INode source, INode? replica)
        {
            if (replica == null)
            {
                FlagNode(source);

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
                    FlagNode(source);
                    return false;
                }
                ;

                // files? - compare
                if (source is FileNode file && replica is FileNode fileSync)
                {
                    bool result = CheckFile(file, fileSync);
                    if (!result) FlagNode(source);
                    return result;
                }

                // folders? - compare content
                if (source is FolderNode folder && replica is FolderNode folderSync)
                {
                    bool result = CheckFolder(folder, folderSync);
                    if (!result) FlagNode(source);
                    return result;
                }

                // same name?
                if (source.Name != replica.Name)
                {
                    FlagNode(source);
                    return false;
                }
            }
            return true;
        }
        private bool CheckFolder(FolderNode source, FolderNode tosync)
        {
            // Start by iterating over all nodes in the folder
            foreach (var node in source.content)
            {
                // find a match by name
                var match = tosync.content.SingleOrDefault<INode>(x => x.Name == node.Name);

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

        private bool CheckFile(FileNode source, FileNode sync)
        {
            // same name, or timestamp match?
            if (source.Name != sync.Name || source.modified > sync.modified)
            {
                _unsynced.Add(source);
                return false;
            }
            return true;
        }

        private void FlagNode(INode node)
        {
            node.IsSynced = false;
            _unsynced.Add(node);
        }
        #endregion

        public void Dispose()
        {
            _timerService.TimeIsUp -= Synchronize;
            _timerService.Dispose();
        }
    }
}
