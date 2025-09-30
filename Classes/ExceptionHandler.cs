namespace SyncAppVeeam.Classes
{
    public static class ExceptionHandler
    {
        //I went with this to not litter code with extra catch blocks
        //Non-critical errors - like file copy/delete/create get ignored but logged accordingly
        //Crititcal errors throw exception likely ending execution
        public static void HandleException(Exception ex, string message = "", bool critical = false)
        {
            var exception = ex.GetType();
            if (critical)
            {
                UserCLIService.CLIPrint($"[CRITICAL] Encountered a critical {exception.ToString()} - program cannot continue. \n{ex.Message}", UserCLIService.InfoType.ERROR);
                Environment.Exit(1);
            }
            else
            {
                UserCLIService.CLIPrint(message == "" ? $"Encountered a {exception.ToString()} - {ex.Message}" : $"{message} - {ex.Message}", UserCLIService.InfoType.ERROR);
            }
        }
    }
}
