namespace SyncAppVeeam.Models
{
    public class FileNode : IEntry
    {
        public string Name { get; set; }
        public string NodePath { get; set; }

        public FileNode(string Name, string Path)
        {
            this.Name = Name;
            this.NodePath = Path;
        }

        public void PrintContent()
        {
            Console.WriteLine($"    {this.Name}");
        }
    }
}
