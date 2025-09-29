using SyncAppVeeam.Classes;
using System.Security.Cryptography;

namespace SyncAppVeeam.Models
{
    public class FileNode : INode
    {
        public string Name { get; set; }
        public string NodePath { get; set; }
        public DateTime modified { get; set; }
        public bool IsSynced { get; set; } = true;
        public bool IsReplica { get; set; }

        public FileNode(string Path, bool IsReplica = false)
        {
            this.Name = System.IO.Path.GetFileName(Path);
            this.NodePath = Path;
            this.IsReplica = IsReplica;
            modified = File.GetLastWriteTime(NodePath);
        }

        public byte[] GetHash()
        {
            using (var md = MD5.Create())
            using (var stream = File.OpenRead(NodePath))
            {
                return md.ComputeHash(stream);
            }
        }

        public long GetSize()
        {
            return new FileInfo(NodePath).Length;
        }

        public void PrintContent(string indent = "")
        {
            UserCLIService.CLIPrint($"{indent}{this.Name} - {IsSynced}");
        }
    }
}
