using dotBento.Infrastructure.Commands.Profile;

namespace dotBento.Infrastructure.Tests.Commands.Profile;

public class ProfileStyleHelperTests
{
    [Theory]
    [InlineData("#FF0000", 100, "#FF0000FF")]
    [InlineData("#00FF00", 50, "#00FF0080")]
    [InlineData("#0000FF", 0, "#0000FF00")]
    [InlineData("#AABBCC", null, "#AABBCCFF")]
    public void ToColorWithOpacity_ShouldCombineColorAndOpacity(string color, int? opacity, string expected)
    {
        // Act
        var result = ProfileStyleHelper.ToColorWithOpacity(color, opacity);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(100, "FF")]
    [InlineData(50, "80")] // 127.5 rounded to 128 = 0x80
    [InlineData(0, "00")]
    [InlineData(null, "FF")]
    [InlineData(75, "BF")] // 191.25 rounded to 191 = 0xBF
    public void ConvertOpacityToHex_ShouldConvertPercentageToHex(int? opacity, string expected)
    {
        // Act
        var result = ProfileStyleHelper.ConvertOpacityToHex(opacity);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Short", "24px")]
    [InlineData("Medium Username", "24px")]
    [InlineData("ThisIsLongerUsername", "18px")]
    [InlineData("ThisIsAnEvenLongerUsernam", "15px")]
    [InlineData("ThisIsAnExtremelyLongUsernameValue", "11px")]
    public void GetUsernameFontSize_ShouldReturnCorrectSize(string username, string expected)
    {
        // Act
        var result = ProfileStyleHelper.GetUsernameFontSize(username);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(10, "16px")]
    [InlineData(36, "16px")]
    [InlineData(40, "14px")]
    [InlineData(46, "12px")]
    [InlineData(100, "10px")]
    public void GetLastFmTextFontSize_ShouldReturnCorrectSize(int textLength, string expected)
    {
        // Act
        var result = ProfileStyleHelper.GetLastFmTextFontSize(textLength);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetEmoteHtml_ShouldGenerateCorrectImageTag()
    {
        // Arrange
        const string emoteUrl = "https://example.com/emote.png";
        const string expected = "<img src=\"https://example.com/emote.png\" width=\"24\" height=\"24\">";

        // Act
        var result = ProfileStyleHelper.GetEmoteHtml(emoteUrl);

        // Assert
        Assert.Equal(expected, result);
    }
}
