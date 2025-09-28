using SyncAppVeeam.Models;

namespace SyncAppVeeam.Classes
{
    public class SynchronizationService : IDisposable
    {
        //This class is responsible on running the actual synchronization process, therefore it receives a list of IEntry objects that are out of sync
        private List<FolderNode> dirs = new();
        private List<FileNode> files = new();
        private string sourceRoot;
        private string destinationRoot;
        private TimerService timerService;
        public SynchronizationService(List<IEntry> tosync, string destinationRoot, string sourceRoot)
        {
            this.destinationRoot = destinationRoot;
            this.sourceRoot = sourceRoot;
            UpdateEntries(tosync);
        }

        public void UpdateEntries(List<IEntry> tosync)
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
            Console.WriteLine("Synchronizing nodes:");
            foreach (var dir in dirs)
            {
                Console.WriteLine(dir.NodePath);
                CopyPasteNode(dir);
            }
            foreach (var file in files)
            {
                Console.WriteLine(file.NodePath);
                CopyPasteNode(file);
            }
        }
        //Run copy
        public void CopyPasteNode(IEntry node)
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
        // Returns a correct new path in destination by taking the source path and swapping it with the destination
        private string ProcessNodePath(IEntry node)
        {
            // get relative path to node
            var relativePath = Path.GetRelativePath(sourceRoot, node.NodePath);
            // add destination path to relative path
            return Path.Combine(destinationRoot, relativePath);

        }

        public void SyncNode()
        {

        }

        public void Dispose()
        {

        }
    }
}
