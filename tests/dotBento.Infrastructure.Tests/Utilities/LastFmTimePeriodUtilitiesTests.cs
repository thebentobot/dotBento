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
}
