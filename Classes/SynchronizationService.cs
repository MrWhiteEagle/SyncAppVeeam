using SyncAppVeeam.Models;

namespace SyncAppVeeam.Classes
{
    public class SynchronizationService
    {
        //This class is responsible on running the actual synchronization process, therefore it receives a list of IEntry objects that are out of sync
        private List<FolderNode> dirs = new();
        private List<FileNode> files = new();
        private string sourceRoot;
        private string destinationRoot;
        public SynchronizationService(List<INode> tosync, string destinationRoot, string sourceRoot)
        {
            this.destinationRoot = destinationRoot;
            this.sourceRoot = sourceRoot;
            UpdateEntries(tosync);
        }

        public void UpdateEntries(List<INode> tosync)
        {
            dirs.Clear();
            files.Clear();
            //Segregate files and folders - folders need to be synced first to keep paths intact
            foreach (var folder in tosync.OfType<FolderNode>())
            {
                dirs.Add(folder);
            }
            foreach (var file in tosync.OfType<FileNode>())
            {
                files.Add(file);
            }
        }

        public void RunSync()
        {
            if (dirs.Count == 0 && files.Count == 0)
            {
                UserCLIService.CLIPrint("All good - everything in sync.");
                return;
            }
            UserCLIService.CLIPrint("Synchronizing nodes:");
            foreach (var dir in dirs)
            {
                UserCLIService.CLIPrint(dir.NodePath);
                SyncNode(dir);
            }
            foreach (var file in files)
            {
                UserCLIService.CLIPrint(file.NodePath);
                SyncNode(file);
            }
        }
        //Run copy
        public void SyncNode(INode node)
        {
            try
            {
                if (node is FolderNode)
                {
                    //When folder is marked as not synced, but exists CreateDirectory has no effect
                    Directory.CreateDirectory(ProcessNodePath(node));
                }
                else
                {
                    //Copy file with override
                    File.Copy(node.NodePath, ProcessNodePath(node), true);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                UserCLIService.CLIPrint($"Error while trying to sync node in: {node.NodePath}. Access denied.");
            }
            catch (Exception ex)
            {
                UserCLIService.CLIPrint($"Error while trying to sync node: {ex.Message}");
            }
        }

        // Returns a correct new path in destination by taking the source path and swapping it with the destination
        private string ProcessNodePath(INode node)
        {
            // get relative path to node
            var relativePath = Path.GetRelativePath(sourceRoot, node.NodePath);
            // add destination path to relative path
            return Path.Combine(destinationRoot, relativePath);

        }
    }
}
