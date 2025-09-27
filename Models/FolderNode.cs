namespace SyncAppVeeam.Models
{
    public class FolderNode : IEntry
    {
        public string Name { get; set; }
        public string NodePath { get; set; }

        public List<IEntry> content = new();

        public FolderNode(string Name, string Path)
        {
            this.Name = Name;
            this.NodePath = Path;
            content = GetContent();
        }

        // Recursive function that assigns a List of entries to the folders content.
        public List<IEntry> GetContent()
        {
            List<IEntry> entries = new();
            try
            {
                // I was wondering if i could use SearchOptions.AllDirectories, but this gives me a list of all possible entries - and i want a tree.
                var nodes = Directory.GetFileSystemEntries(NodePath);
                foreach (var node in nodes)
                {
                    if (Directory.Exists(node))
                    {
                        var dir = new FolderNode(Path.GetFileName(node), node);
                        dir.GetContent();
                        entries.Add(dir);
                    }
                    else if (File.Exists(node))
                    {
                        var file = new FileNode(Path.GetFileName(node), node);
                        entries.Add(file);
                    }
                }

            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Access to path: {NodePath} was denied. {ex.Message}");
            }
            return entries;
        }

        public void PrintContent()
        {
            Console.WriteLine($"/{this.Name}");
            foreach (var entry in content)
            {
                entry.PrintContent();
            }
        }
    }
}
