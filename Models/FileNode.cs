namespace SyncAppVeeam.Models
{
    public class FileNode : IEntry
    {
        public string Name { get; set; }
        public string NodePath { get; set; }
        public DateTime modified { get; set; }
        public bool IsSynced { get; set; } = true;

        public FileNode(string Name, string Path)
        {
            this.Name = Name;
            this.NodePath = Path;
            //Geting last write time to file to check changes later in comparison.
            modified = File.GetLastWriteTime(NodePath);
        }

        public void PrintContent(string indent = "")
        {

            Console.Write($"{indent}{this.Name} - {IsSynced}");
            Console.WriteLine($"\tModified: {modified.ToString()}");
        }
    }
}
