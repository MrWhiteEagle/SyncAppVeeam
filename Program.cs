using SyncAppVeeam.Classes;
using System.Text.RegularExpressions;

public class Program
{
    static void Main()
    {
        string[] args = { "--source", "C:\\Users\\MrWhiteEagle\\Documents\\VSPROJ\\SyncAppVeeam\\TestEnvSource", "--path", "C:\\Users\\MrWhiteEagle\\Documents\\VSPROJ\\SyncAppVeeam\\TestEnvSync", "--interval", "10s10m48h" };
        Setup(args);

    }

    static void Setup(string[] args)
    {
        string SourcePath = "/";
        string SyncPath = "/";
        TimeSpan interval = TimeSpan.Zero;

        //checking every second argument to keep "--" notation.
        for (int i = 0; i < args.Length; i += 2)
        {
            switch (args[i])
            {
                case "--source":
                    SourcePath = args[i + 1]; break;
                case "--path":
                    SyncPath = args[i + 1]; break;
                case "--interval":
                    interval = ParseInterval(args[i + 1]); break;

            }
        }

        SyncManagerService syncService = new SyncManagerService(SourcePath, SyncPath, interval);

    }

    static TimeSpan ParseInterval(string interval)
    {
        TimeSpan result = TimeSpan.Zero;

        // Process interval notation into arrays of time and units of time;
        // Units - using regex, allowing nothing but smhDMY - the notation characters (I also neede to add the whitespace check since it was returning spaces and empty strings
        var units = Regex.Split(interval, @"[^smhDMY]").Select(u => u).Where(u => !String.IsNullOrWhiteSpace(u) && !String.IsNullOrEmpty(u)).ToArray();

        // Spans - using regex again to split input into digits, that are supposed to match the units.
        var spans = Regex.Matches(interval, @"\d+").Select(s => int.Parse(s.Value)).ToArray();

        // If number of time spans is not equal to number of units, throw exception.
        if (units.Length != spans.Length)
        {
            throw new ArgumentException("Invalid time provided - use XXsXXmXXhXXDXXMXXY notation.");
        }

        for (int i = 0; i < units.Length; i++)
        {
            //For each instance of a unit, add its value to the total timespan
            result += units[i] switch
            {
                "s" => TimeSpan.FromSeconds(spans[i]),
                "m" => TimeSpan.FromMinutes(spans[i]),
                "h" => TimeSpan.FromHours(spans[i]),
                "D" => TimeSpan.FromDays(spans[i]),
                "M" => TimeSpan.FromDays(spans[i] * 30), // <-- Approximation
                "Y" => TimeSpan.FromDays(spans[i] * 365), // <-- Again
                _ => TimeSpan.Zero // <-- Any other value (if it even manages to come through the regex) is ignored

            };
        }
        return result;
    }
}
