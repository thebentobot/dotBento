using dotBento.Bot.Utilities;

namespace dotBento.Bot.Tests.Utilities;

public class StringUtilitiesTests
{
    [Theory]
    [InlineData("Chris", "Chris'")]
    [InlineData("James", "James'")]
    [InlineData("Alice", "Alice's")]
    [InlineData("boss", "boss'")]
    [InlineData("cat", "cat's")]
    [InlineData("", "")]
    [InlineData(null, null)]
    public void AddPossessiveS_WorksAsExpected(string? input, string? expected)
    {
        var result = StringUtilities.AddPossessiveS(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GenerateRandomCode_Returns8CharAlphanumeric()
    {
        var code = StringUtilities.GenerateRandomCode();

        Assert.NotNull(code);
        Assert.Equal(8, code.Length);
        Assert.Matches("^[A-Z1-9]+$", code);
    }

    [Fact]
    public void GenerateRandomCode_ReturnsDifferentResults()
    {
        var code1 = StringUtilities.GenerateRandomCode();
        var code2 = StringUtilities.GenerateRandomCode();

        Assert.NotEqual(code1, code2);
    }
}