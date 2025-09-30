using System.Text;

namespace SyncAppVeeam.Classes
{
    public static class UserCLIService
    {
        public enum InfoType
        {
            INFO,
            ERROR,
            CREATE,
            DELETE,
            COPY
        }
        private static StringBuilder logbuilder = new StringBuilder();
        // Automatically set the output file on directory set
        private static string logFilePath = "";
        private static string logDirPath = "";
        public static string logPath
        {
            get => logFilePath;
            set
            {
                logDirPath = value;
                logFilePath = Path.Combine(value, $"log-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");
            }
        }
        public static void Start()
        {
            CLIPrint($"Program started at: {DateTime.Now}");

            // Start with default file path in case of errors
            logPath = "./";
        }

        public static void Stop()
        {
            LogToFile();
        }

        public static void CLIPrint(string message, InfoType type = InfoType.INFO)
        {
            logbuilder.AppendLine($"[{type.ToString()}]{message}");
            Console.WriteLine($"[{type.ToString()}]{message}");
        }

        public static void LogToFile()
        {
            try
            {
                Directory.CreateDirectory(logDirPath);
                using (var writer = File.AppendText(logFilePath))
                {
                    writer.Write(logbuilder.ToString());
                }
                logbuilder.Clear();
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "Couldn't access logfile location, or create a directory thats supposed to contain it.");
            }
        }
    }
}
