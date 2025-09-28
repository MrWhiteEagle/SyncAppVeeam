using SyncAppVeeam.Models;

namespace SyncAppVeeam.Classes
{
    public sealed class SyncManagerService : IDisposable
    {
        private FolderNode _sourceNode;
        private FolderNode _destinationNode;
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

            //populate the nodes again
            this._sourceNode = GetFileTree(_sourceRoot);
            this._destinationNode = GetFileTree(_destinationRoot, true);
            var sourceDirs = GetDirectories(_sourceNode);
            var destinationDirs = GetDirectories(_destinationNode);
            PrintFileTree();

            foreach (var dir in GetDirectories(_sourceNode).OrderBy(x => x.NodePath))
            {
                CheckFolder(dir);
            }
            foreach (var dir in GetDirectories(_destinationNode).OrderBy(x => x.NodePath))
            {
                CheckFolder(dir);
            }
            var syncService = new SynchronizationService(GetDirectories(_sourceNode).OrderBy(x => x.NodePath).ToList(), GetDirectories(_destinationNode).OrderBy(x => x.NodePath).ToList(), _destinationRoot, _sourceRoot);
        }

        private FolderNode GetFileTree(string path, bool replica = false)
        {
            FolderNode? result = null;
            //Check if provided path is a dir
            if (Directory.Exists(path))
            {
                result = new FolderNode(Path.GetFileName(path), path, "\\", true, replica);
            }
            return result ?? throw new InvalidOperationException($"Provided path: {path} does not exist or is not a directory");
        }

        List<FolderNode> GetDirectories(FolderNode root)
        {
            var list = new List<FolderNode>();
            list.Add(root);
            foreach (var folder in root.content.OfType<FolderNode>())
            {
                list.AddRange(GetDirectories(folder));
            }
            return list.OrderBy(x => x.NodePath).ToList();
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

        public void Dispose()
        {
            _timerService.TimeIsUp -= Synchronize;
            _timerService.Dispose();
        }

        private void CheckFolder(FolderNode folder)
        {
            foreach (var file in folder.content.OfType<FileNode>())
            {
                CheckFile(file);
            }

            var relativePath = Path.GetRelativePath(folder.IsReplica ? _destinationNode.NodePath : _sourceNode.NodePath, folder.NodePath);
            if (Directory.Exists(Path.Combine(folder.IsReplica ? _sourceNode.NodePath : _destinationNode.NodePath, relativePath)))
            {
                return;
            }
            else
            {
                folder.IsSynced = false;
            }
        }

        private void CheckFile(FileNode file)
        {
            var relativePath = Path.GetRelativePath(file.IsReplica ? _destinationNode.NodePath : _sourceNode.NodePath, file.NodePath);
            if (File.Exists(Path.Combine(file.IsReplica ? _sourceNode.NodePath : _destinationNode.NodePath, relativePath)))
            {
                return;
            }
            else
            {
                file.IsSynced = false;
            }
        }
    }
}
