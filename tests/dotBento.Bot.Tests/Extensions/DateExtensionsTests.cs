using dotBento.Bot.Extensions;

namespace dotBento.Bot.Tests.Extensions;

public sealed class DateExtensionsTests
{
    [Fact]
    public void ParseDateTimeOffset_ReturnsValueForValidDate()
    {
        var result = "2026-06-06T12:30:00+02:00".ParseDateTimeOffset();

        Assert.True(result.HasValue);
        Assert.Equal(TimeSpan.FromHours(2), result.Value.Offset);
    }

    [Fact]
    public void ParseDateTimeOffset_ReturnsNoneForInvalidDate()
    {
        var result = "not a date".ParseDateTimeOffset();

        Assert.True(result.HasNoValue);
    }
}
