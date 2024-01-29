using dotBento.Infrastructure.Models.LastFm;

namespace dotBento.Infrastructure.Utilities;

public static class LastFmTimePeriodUtilities
{
    public static Dictionary<string, string?> UserOptionsLastFmTimeSpanSlashCommand = new()
    {
        {"Overall", LastFmTimeSpan.Overall},
        {"7 Days", LastFmTimeSpan.Week},
        {"1 Month", LastFmTimeSpan.Month},
        {"3 Months", LastFmTimeSpan.Quarter},
        {"6 Months", LastFmTimeSpan.HalfYear},
        {"1 Year", LastFmTimeSpan.Year}
    };
    
    public static Dictionary<string, string?> UserOptionsLastFmTimeSpanTextCommand = new()
    {
        {"all", LastFmTimeSpan.Overall},
        {"week", LastFmTimeSpan.Week},
        {"month", LastFmTimeSpan.Month},
        {"quarter", LastFmTimeSpan.Quarter},
        {"half", LastFmTimeSpan.HalfYear},
        {"year", LastFmTimeSpan.Year}
    };
    
    public static string LastFmTimeSpanFromUserOptionSlashCommand(string userOption)
    {
        UserOptionsLastFmTimeSpanSlashCommand.TryGetValue(userOption, out var lastFmTimeSpan);
        return lastFmTimeSpan ?? LastFmTimeSpan.Overall;
    }
    
    public static string? LastFmTimeSpanFromUserOptionTextCommand(string userOption)
    {
        UserOptionsLastFmTimeSpanTextCommand.TryGetValue(userOption, out var lastFmTimeSpan);
        return lastFmTimeSpan;
    }
}