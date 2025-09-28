using SyncAppVeeam.Models;

namespace SyncAppVeeam.Classes
{
    public class SynchronizationService
    {
        //This class is responsible on running the actual synchronization process, therefore it receives a list of IEntry objects that are out of sync
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

        public void UpdateEntries(List<FolderNode> source, List<FolderNode> dest)
        {
            sourceDirs.Clear();
            destinationDirs.Clear();
            // add only those nodes, which's children are not synced
            sourceDirs.AddRange(source.Where(
                x => x.content.Any(c => !c.IsSynced)));
            destinationDirs.AddRange(dest.Where(
                x => x.content.Any(c => !c.IsSynced)));
            if (sourceDirs.Count > 0 && destinationDirs.Count > 0)
            {
                foreach (var dir in sourceDirs)
                {
                    UserCLIService.CLIPrint(dir.NodePath + " " + dir.IsSynced);
                    SyncDir(dir);
                }
                foreach (var dir in destinationDirs)
                {
                    UserCLIService.CLIPrint(dir.NodePath + " " + dir.IsSynced);
                    SyncDir(dir);
                }
            }
            else
            {
                UserCLIService.CLIPrint("Everything in sync...");
            }
        }

        // 
        private void SyncDir(FolderNode node)
        {
            // If a node is source
            if (!node.IsReplica)
            {
                // attempt to create a dir (doesnt matter if it exists or not) at the replica
                Directory.CreateDirectory(ProcessNodePath(node));
                UserCLIService.CLIPrint($"Creating directory {Path.GetRelativePath(sourceRoot, node.NodePath)} in destination...");
                // then try to sync each file in that node that is not synced
                foreach (var file in node.content.OfType<FileNode>().Where(x => !x.IsSynced))
                {
                    SyncFile(file);
                }
            }
            // If a node is replica
            else
            {
                // if it is not synced - it means it doesnt exist at all in source - delete recursively
                if (!node.IsSynced)
                {
                    Directory.Delete(node.NodePath, true);
                    UserCLIService.CLIPrint($"Deleting {node.NodePath} in destination...");
                }
                // if synced - try to sync all unsynced files
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
            // If a file is not a replica - it needs to be copied to the destination with overwrite
            if (!node.IsReplica)
            {
                File.Copy(node.NodePath, ProcessNodePath(node), true);
                UserCLIService.CLIPrint($"Copying {node.NodePath} to destination...");
            }
            // If it is, needs to be deleted
            else
            {
                File.Delete(node.NodePath);
                UserCLIService.CLIPrint($"Deleting {node.NodePath} in destination...");
            }
        }

        // Returns a correct new path in destination by taking the source path and swapping it with the destination
        private string ProcessNodePath(INode node)
        {
            // get relative path to node
            var relativePath = Path.GetRelativePath(node.IsReplica ? destinationRoot : sourceRoot, node.NodePath);
            // add destination path to relative path
            return Path.Combine(node.IsReplica ? sourceRoot : destinationRoot, relativePath);

        }
    }
}
