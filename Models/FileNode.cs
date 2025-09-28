using SyncAppVeeam.Classes;

namespace SyncAppVeeam.Models
{
    public class FileNode : INode
    {
        public string Name { get; set; }
        public string NodePath { get; set; }
        public DateTime modified { get; set; }
        public bool IsSynced { get; set; } = true;
        public bool IsReplica { get; set; }

        public FileNode(string Name, string Path, bool IsReplica = false)
        {
            this.Name = Name;
            this.NodePath = Path;
            this.IsReplica = IsReplica;
            //Geting last write time to file to check changes later in comparison.
            modified = File.GetLastWriteTime(NodePath);
        }

        public void PrintContent(string indent = "")
        {

            UserCLIService.CLIPrint($"{indent}{this.Name} - {IsSynced}\tModified: {modified.ToString()}");
        }
    }
}
