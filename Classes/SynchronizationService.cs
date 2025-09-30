using SyncAppVeeam.Models;

namespace SyncAppVeeam.Classes
{
    /// <summary>
    /// Responsible for actual synchronization of directories, receives replica and source dirs to evaluate and act.
    /// First creation always results in sync
    /// </summary>
    public class SynchronizationService
    {
        private List<FolderNode> sourceDirs = new();
        private List<FolderNode> destinationDirs = new();
        private string sourceRoot;
        private string destinationRoot;
        public SynchronizationService(List<FolderNode> source, List<FolderNode> dest, string destinationRoot, string sourceRoot)
        {
            this.destinationRoot = destinationRoot;
            this.sourceRoot = sourceRoot;
            UpdateEntries(source, dest);
        }

        /// <summary>
        /// Updates lists with directories, filters out synchronized nodes, runs sync
        /// </summary>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        public void UpdateEntries(List<FolderNode> source, List<FolderNode> dest)
        {
            sourceDirs.Clear();
            destinationDirs.Clear();

            // add only those nodes, which's children are not synced
            sourceDirs.AddRange(source.Where(
                x => x.content.Any(c => !c.IsSynced)));

            destinationDirs.AddRange(dest.Where(
                x => x.content.Any(c => !c.IsSynced)));

            if (sourceDirs.Count != 0 || destinationDirs.Count != 0)
            {
                //Sync destination first - delete unmatched entries to avoid conflicts with same name files
                foreach (var dir in destinationDirs)
                {
                    UserCLIService.CLIPrint(dir.NodePath + " " + dir.IsSynced);
                    SyncDir(dir);
                }
                foreach (var dir in sourceDirs)
                {
                    UserCLIService.CLIPrint(dir.NodePath + " " + dir.IsSynced);
                    SyncDir(dir);
                }
            }
            else
            {
                UserCLIService.CLIPrint("Everything in sync...");
            }
            UserCLIService.CLIPrint("Press q to quit.");
        }

        // 
        private void SyncDir(FolderNode node)
        {
            // if source
            if (!node.IsReplica)
            {
                // attempt to create a dir (doesnt matter if it exists or not) at the replica
                try
                {
                    Directory.CreateDirectory(ProcessNodePath(node));
                    UserCLIService.CLIPrint($"Creating directory {Path.GetRelativePath(sourceRoot, node.NodePath)} in destination...");
                    // then try to sync each file in that node that is not synced
                    foreach (var file in node.content.OfType<FileNode>().Where(x => !x.IsSynced))
                    {
                        SyncFile(file);
                    }
                }
                catch (Exception ex)
                {
                    ExceptionHandler.HandleException(ex, $"Cannot create directory {node.NodePath}");
                }
            }
            // if replica
            else
            {
                // if not synced - nonexistent in source - delete
                if (!node.IsSynced)
                {
                    try
                    {
                        if (Directory.Exists(node.NodePath))
                        {
                            Directory.Delete(node.NodePath, true);
                            UserCLIService.CLIPrint($"Deleting {node.NodePath} in destination...", UserCLIService.InfoType.DELETE);
                        }
                        else
                        {
                            UserCLIService.CLIPrint($"Directory {node.NodePath} was already deleted - skipping");
                        }

                    }
                    catch (Exception ex)
                    {
                        ExceptionHandler.HandleException(ex, $"Cannot delete directory {node.NodePath}");
                    }
                }
                // if synced - sync files inside
                else
                {
                    foreach (var file in node.content.OfType<FileNode>().Where(x => !x.IsSynced))
                    {
                        SyncFile(file);
                    }
                }
            }
        }

        private void SyncFile(FileNode node)
        {
            // if source - copy
            if (!node.IsReplica)
            {
                try
                {
                    File.Copy(node.NodePath, ProcessNodePath(node), true);
                    UserCLIService.CLIPrint($"Copying {node.NodePath} to destination...", UserCLIService.InfoType.COPY);
                }
                catch (Exception ex)
                {
                    ExceptionHandler.HandleException(ex, $"Cannot copy {node.NodePath}");
                }
            }
            // if replica - delete
            else
            {
                try
                {
                    if (File.Exists(node.NodePath))
                    {
                        File.Delete(node.NodePath);
                        UserCLIService.CLIPrint($"Deleting {node.NodePath} in destination...", UserCLIService.InfoType.DELETE);
                    }
                    else
                    {
                        UserCLIService.CLIPrint($"File {node.NodePath} was already deleted - skipping");
                    }

                }
                catch (Exception ex)
                {
                    ExceptionHandler.HandleException(ex, $"Cannot delete {node.NodePath}");
                }
            }
        }

        /// <summary>
        /// Returns node's supposed counterpart path.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private string ProcessNodePath(INode node)
        {
            // get relative path to node
            var relativePath = Path.GetRelativePath(node.IsReplica ? destinationRoot : sourceRoot, node.NodePath);
            // add destination path to relative path
            return Path.Combine(node.IsReplica ? sourceRoot : destinationRoot, relativePath);

        }
    }
}
