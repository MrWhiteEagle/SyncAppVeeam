using SyncAppVeeam.Classes;

namespace SyncAppVeeam.Models
{
    public class FolderNode : INode
    {
        public string Name { get; set; }
        public string NodePath { get; set; }
        public bool IsSynced { get; set; } = true;
        public bool IsReplica { get; set; }
        public bool IsRootDir { get; set; }

        public List<INode> content = new();

        public FolderNode(string Path, bool IsRootDir = false, bool IsReplica = false)
        {
            this.Name = System.IO.Path.GetFileName(Path);
            this.NodePath = Path;
            this.IsRootDir = IsRootDir;
            this.IsReplica = IsReplica;
            content = GetContent(this.IsReplica);
        }

        public List<INode> GetContent(bool IsReplica = false)
        {
            List<INode> entries = new();
            var nodes = Directory.GetFileSystemEntries(NodePath);
            foreach (var node in nodes)
            {
                try
                {
                    // Get all nodes for the folder;
                    // If its a folder - its constructor recursively fetches its content, if its a file, creaste a filenode and add to the folders content
                    if (Directory.Exists(node))
                    {
                        var dir = new FolderNode(node, false, IsReplica);
                        entries.Add(dir);
                    }
                    else if (File.Exists(node))
                    {
                        var file = new FileNode(node, this.IsReplica);
                        entries.Add(file);
                    }
                }
                // safeguard on directory access
                catch (UnauthorizedAccessException ex)
                {
                    UserCLIService.CLIPrint($"Access to path: {NodePath} was denied. {ex.Message}", UserCLIService.InfoType.ERROR);
                }
            }

            // files first
            return entries.OrderBy(x => x is FolderNode).ThenBy(x => x.Name).ToList();
        }

        // Using indent like this is a cluncky solution but i couldnt come up with a better one
        public void PrintContent(string indent = "")
        {
            UserCLIService.CLIPrint($"{indent}\\{this.Name} - {this.IsSynced}");
            foreach (var entry in content)
            {
                entry.PrintContent(indent + "\t");
            }
        }
    }
}
