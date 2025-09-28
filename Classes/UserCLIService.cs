using System.Text;

namespace SyncAppVeeam.Classes
{
    public static class UserCLIService
    {
        private static StringBuilder logbuilder = new StringBuilder();
        public static void Start()
        {
            CLIPrint($"Program started at: {DateTime.Now}");
        }

        public static void Stop()
        {
            LogToFile();
        }

        public static void CLIPrint(string message)
        {
            logbuilder.AppendLine(message);
            Console.WriteLine(message);
        }

        public static void LogToFile()
        {
            Directory.CreateDirectory("./SyncLog");
            using (var writer = File.CreateText($"./SyncLog/log-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt"))
            {
                writer.Write(logbuilder.ToString());
            }
        }
    }
}
