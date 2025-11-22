using dotBento.Infrastructure.Models.LastFm;
using dotBento.Infrastructure.Utilities;

namespace dotBento.Infrastructure.Tests.Utilities;

public class LastFmTimePeriodUtilitiesTests
{
    public static IEnumerable<object[]> SlashCommandOptions =>
        new List<object[]>
        {
            new object[] { "Overall", LastFmTimeSpan.Overall },
            new object[] { "7 Days", LastFmTimeSpan.Week },
            new object[] { "1 Month", LastFmTimeSpan.Month },
            new object[] { "3 Months", LastFmTimeSpan.Quarter },
            new object[] { "6 Months", LastFmTimeSpan.HalfYear },
            new object[] { "1 Year", LastFmTimeSpan.Year }
        };

    public static IEnumerable<object[]> TextCommandOptions =>
        new List<object[]>
        {
            new object[] { "all", LastFmTimeSpan.Overall },
            new object[] { "week", LastFmTimeSpan.Week },
            new object[] { "month", LastFmTimeSpan.Month },
            new object[] { "quarter", LastFmTimeSpan.Quarter },
            new object[] { "half", LastFmTimeSpan.HalfYear },
            new object[] { "year", LastFmTimeSpan.Year }
        };

    [Theory]
    [MemberData(nameof(SlashCommandOptions))]
    public void LastFmTimeSpanFromUserOptionSlashCommand_ReturnsCorrectValue(string input, string expected)
    {
        var result = LastFmTimePeriodUtilities.LastFmTimeSpanFromUserOptionSlashCommand(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void LastFmTimeSpanFromUserOptionSlashCommand_ReturnsOverallForUnknownValue()
    {
        var result = LastFmTimePeriodUtilities.LastFmTimeSpanFromUserOptionSlashCommand("invalid_option");
        Assert.Equal(LastFmTimeSpan.Overall, result);
    }

    [Theory]
    [MemberData(nameof(TextCommandOptions))]
    public void LastFmTimeSpanFromUserOptionTextCommand_ReturnsCorrectValue(string input, string expected)
    {
        var result = LastFmTimePeriodUtilities.LastFmTimeSpanFromUserOptionTextCommand(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void LastFmTimeSpanFromUserOptionTextCommand_ReturnsNullForUnknownValue()
    {
        var result = LastFmTimePeriodUtilities.LastFmTimeSpanFromUserOptionTextCommand("invalid_option");
        Assert.Null(result);
    }
}
