namespace SyncAppVeeam.Models
{
    public interface INode
    {
        public string Name { get; set; }
        public string NodePath { get; set; }
        public bool IsSynced { get; set; }

        public void PrintContent(string indent = "") { }
    }
}
