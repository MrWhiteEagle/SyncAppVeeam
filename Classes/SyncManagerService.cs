using SyncAppVeeam.Models;
using System.Security.Cryptography;

namespace SyncAppVeeam.Classes
{
    /// <summary>
    /// Responsible for fetching file trees, marking them as synced or not, and calling SyncService, automatically calls the sync on first creation.
    /// Has a TimerService to handle ticks
    /// </summary>
    public sealed class SyncManagerService : IDisposable
    {
        private FolderNode _sourceNode;
        private FolderNode _destinationNode;
        private SynchronizationService _syncService;
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

            //Force first sync
            _timerService.ForceTick();
        }

        /// <summary>
        /// Update file trees, fetch directories, run synchronization. bound to timer's "Elapsed" event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Synchronize(object? sender, EventArgs e)
        {
            UserCLIService.CLIPrint($"Sync Requested at {DateTime.Now}");

            //Create file trees, mark replicas
            this._sourceNode = GetFileTree(_sourceRoot);
            this._destinationNode = GetFileTree(_destinationRoot, true);

            //Get all directories in trees
            var sourceDirs = GetDirectories(_sourceNode);
            var destinationDirs = GetDirectories(_destinationNode);

            //Check all folders and files - mark them as synced or not
            foreach (var dir in sourceDirs)
            {
                CheckFolder(dir);
            }
            foreach (var dir in destinationDirs)
            {
                CheckFolder(dir);
            }

            // For convenience
            PrintFileTree();


            // Create syncservice if it doesnt exist, if it does, update entries - then run
            if (_syncService == null)
            {
                _syncService = new SynchronizationService(sourceDirs, destinationDirs, _destinationRoot, _sourceRoot);
            }
            else
            {
                _syncService.UpdateEntries(sourceDirs, destinationDirs);
            }

            // Log results
            UserCLIService.LogToFile();
        }

        private FolderNode GetFileTree(string path, bool replica = false)
        {
            FolderNode? result = null;

            //Check if provided path is a dir
            if (Directory.Exists(path))
            {
                result = new FolderNode(Path.GetFileName(path), path, true, replica);
            }

            //return result, if not a directory - throw
            //we cant continue here, becuase the paths are provided at start and not changed - therefore throw and not try/catch
            return result ?? throw new DirectoryNotFoundException($"Provided path: {path} does not exist or is not a directory");
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


        private void CheckFolder(FolderNode folder)
        {
            //Look for any file nodes in directory
            foreach (var file in folder.content.OfType<FileNode>())
            {
                CheckFile(file);
            }

            //Check if directory exists in counterpart, if not - flag it
            var relativePath = Path.GetRelativePath(folder.IsReplica ? _destinationNode.NodePath : _sourceNode.NodePath, folder.NodePath);
            if (!Directory.Exists(Path.Combine(folder.IsReplica ? _sourceNode.NodePath : _destinationNode.NodePath, relativePath)))
            {
                folder.IsSynced = false;
            }
        }

        private void CheckFile(FileNode file)
        {
            //Check if file exists in counterpart - if yes leave it, else flag
            var relativePath = Path.GetRelativePath(file.IsReplica ? _destinationNode.NodePath : _sourceNode.NodePath, file.NodePath);
            var counterPath = Path.Combine(file.IsReplica ? _sourceNode.NodePath : _destinationNode.NodePath, relativePath);
            //I'm going to leave hash check but personally - I think timestamp check is enough. Especially with large files, it would be more performant
            //Hash check could be useful if someone deliberately set the timestamp on a file, but as well we could have the same name files with different content and same timestamp - although achieving that is hard
            if (File.Exists(counterPath))
            {
                using (var md = MD5.Create())
                using (var stream = File.OpenRead(counterPath))
                {
                    if (!md.ComputeHash(stream).SequenceEqual(file.GetHash()))
                    {
                        file.IsSynced = false;
                    }
                }
                //if (file.modified > File.GetLastWriteTime(counterPath))
                //{
                //    file.IsSynced = false;
                //}
            }
            else
            {
                file.IsSynced = false;
            }
        }
        //Unsubscribe to timer, and dispose
        public void Dispose()
        {
            _timerService.TimeIsUp -= Synchronize;
            _timerService.Dispose();
        }
    }
}
