using dotBento.Infrastructure.Models.LastFm;

namespace dotBento.Infrastructure.Utilities;

public static class LastFmTimePeriodUtilities
{
    public static Dictionary<string, string?> UserOptionsLastFmTimeSpan = new()
    {
        {"Overall", LastFmTimeSpan.Overall},
        {"7 Days", LastFmTimeSpan.Week},
        {"1 Month", LastFmTimeSpan.Month},
        {"3 Months", LastFmTimeSpan.Quarter},
        {"6 Months", LastFmTimeSpan.HalfYear},
        {"1 Year", LastFmTimeSpan.Year}
    };
    
    public static string LastFmTimeSpanFromUserOption(string userOption)
    {
        UserOptionsLastFmTimeSpan.TryGetValue(userOption, out var lastFmTimeSpan);
        return lastFmTimeSpan ?? LastFmTimeSpan.Overall;
    }
}