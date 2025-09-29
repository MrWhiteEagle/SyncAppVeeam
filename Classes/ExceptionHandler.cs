namespace SyncAppVeeam.Classes
{
    public static class ExceptionHandler
    {
        public static void HandleException(Exception ex, string message = "", bool critical = false)
        {
            var exception = ex.GetType();
            if (critical)
            {
                throw new Exception($"[CRITICAL] Encountered a critical {exception.ToString()} - program cannot continue. \n{ex.Message}");
            }
            else
            {
                UserCLIService.CLIPrint(message == "" ? $"Encountered a {exception.ToString()} - {ex.Message}" : $"{message} - {ex.Message}", UserCLIService.InfoType.ERROR);
            }
        }
    }
}
