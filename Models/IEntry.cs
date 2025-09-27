namespace SyncAppVeeam.Models
{
    public interface IEntry
    {
        public string Name { get; set; }
        public string NodePath { get; set; }

        public void PrintContent() { }
    }
}
