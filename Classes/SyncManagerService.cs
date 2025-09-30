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
            this._sourceRoot = Path.GetFullPath(source).TrimEnd(Path.DirectorySeparatorChar);
            this._destinationRoot = Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar);

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
            _timerService.Lock();
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
            _timerService.Release();
        }

        private FolderNode GetFileTree(string path, bool replica = false)
        {
            FolderNode? result = null;

            //Check if provided path is a dir, if not attempt to create it
            if (Directory.Exists(path))
            {
                try
                {
                    result = new FolderNode(path, true, replica);
                }
                catch
                {
                    //if cannot be accessed - exit
                    //we cant continue here, because the paths are provided at start and not changed - therefore exit
                    this.Dispose();
                    ExceptionHandler.HandleException(new UnauthorizedAccessException($"Provided path: {path} to one of the root nodes could not be accessed"), "", true);
                }
            }
            else
            {
                try
                {
                    Directory.CreateDirectory(path);
                    result = new FolderNode(path, true, replica);
                }
                catch
                {
                    ExceptionHandler.HandleException(new UnauthorizedAccessException($"Provided directory: {path} doesnt exist and could not be created"), "", true);
                }
            }
            //If somehow previous chacks fail to notice result not returning or a path could not be aquired - end
            if (result == null || result.NodePath == null)
            {
                ExceptionHandler.HandleException(new DirectoryNotFoundException($"Could not receive contents of provided directory {path}, or failed to create it, cannot continue."), "", true);
            }
            //Null checked above ^^^
            return result!;
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
            //Check if directory exists in counterpart, if not - flag it
            var relativePath = Path.GetRelativePath(folder.IsReplica ? _destinationRoot : _sourceRoot, folder.NodePath);
            var counterpath = Path.GetFullPath(Path.Combine(folder.IsReplica ? _sourceRoot : _destinationRoot, relativePath));

            if (!Directory.Exists(counterpath))
            {
                folder.IsSynced = false;
            }
            //Look for any file nodes in directory
            foreach (var file in folder.content.OfType<FileNode>())
            {
                CheckFile(file);
            }
        }

        private void CheckFile(FileNode file)
        {
            //Check if file exists in counterpart - if yes leave it, else flag
            var relativePath = Path.GetRelativePath(file.IsReplica ? _destinationRoot : _sourceRoot, file.NodePath);
            var counterPath = Path.GetFullPath(Path.Combine(file.IsReplica ? _sourceRoot : _destinationRoot, relativePath));

            //First check timestamp - if it matches, try to check size - if it matches then try to use MD5
            if (File.Exists(counterPath))
            {

                if (file.modified > File.GetLastWriteTime(counterPath))
                {
                    file.IsSynced = false;
                    return;
                }
                else if (file.GetSize() != new FileInfo(counterPath).Length)
                {
                    file.IsSynced = false;
                    return;
                }
                else
                {
                    using (var md = MD5.Create())
                    using (var stream = File.OpenRead(counterPath))
                    {
                        if (!md.ComputeHash(stream).SequenceEqual(file.GetHash()))
                        {
                            file.IsSynced = false;
                        }
                    }
                }
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
