namespace SyncAppVeeam.Models
{
    public class FolderNode : IEntry
    {
        public string Name { get; set; }
        public string NodePath { get; set; }
        public string ParentPath { get; set; }
        public bool IsSynced { get; set; } = true;

        public bool IsRootDir { get; set; }

        public List<IEntry> content = new();

        public FolderNode(string Name, string Path, string parent, bool IsRootDir = false)
        {
            this.Name = Name;
            this.NodePath = Path;
            this.ParentPath = parent;
            this.IsRootDir = IsRootDir;
            content = GetContent();
        }

        // Recursive function that assigns a List of entries to the folders content.
        public List<IEntry> GetContent()
        {
            List<IEntry> entries = new();
            // I was wondering if i could use SearchOptions.AllDirectories, but this gives me a list of all possible entries - and i want a tree.
            var nodes = Directory.GetFileSystemEntries(NodePath);
            foreach (var node in nodes)
            {
                try
                {
                    // Get all nodes for the folder;
                    // If its a folder - recursively call GetContent(), if its a file, creaste a filenode and add to the folders content
                    if (Directory.Exists(node))
                    {
                        var dir = new FolderNode(Path.GetFileName(node), node, Name);
                        dir.GetContent();
                        entries.Add(dir);
                    }
                    else if (File.Exists(node))
                    {
                        var file = new FileNode(Path.GetFileName(node), node);
                        entries.Add(file);
                    }
                }
                // If we cant reach a folder or file its likely access problem.
                catch (UnauthorizedAccessException ex)
                {
                    Console.WriteLine($"Access to path: {NodePath} was denied. {ex.Message}");
                }
            }

            //Sort the content so it shows files first - then folders and its content
            entries = entries.OrderBy(x => x is FolderNode).ThenBy(x => x.Name).ToList();
            return entries;
        }

        // Using indent like this is a cluncky solution but i couldnt come up with a better one
        public void PrintContent(string indent = "")
        {
            Console.WriteLine($"{indent}{ParentPath}\\{this.Name} - {this.IsSynced}");
            foreach (var entry in content)
            {
                entry.PrintContent(indent + "\t");
            }
        }
    }
}
