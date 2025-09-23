using dotBento.Infrastructure.Utilities;

namespace dotBento.Infrastructure.Tests.Utilities;

public class ProfileValidationUtilitiesTests
{
    // IsValidHttpUrl
    [Theory]
    [InlineData("http://example.com", true)]
    [InlineData("https://example.com", true)]
    [InlineData("https://example.com/image.png", true)]
    [InlineData("ftp://example.com", false)]
    [InlineData("file:///c:/temp/test.txt", false)]
    [InlineData("not a url", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidHttpUrl_WorksAsExpected(string? input, bool expected)
    {
        var result = ProfileValidationUtilities.IsValidHttpUrl(input!);
        Assert.Equal(expected, result);
    }

    // NormalizeHex
    [Theory]
    [InlineData("#1f2937", "#1F2937")]
    [InlineData("1F2937", "#1F2937")]
    [InlineData("  1f2937  ", "#1F2937")]
    [InlineData("#ABCDEF", "#ABCDEF")]
    [InlineData("abcdef", "#ABCDEF")]
    public void NormalizeHex_ValidInputs_ReturnsNormalized(string input, string expected)
    {
        var result = ProfileValidationUtilities.NormalizeHex(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("#12345")] // too short
    [InlineData("1234567")] // too long
    [InlineData("GGGGGG")] // invalid hex chars
    [InlineData("#12G45Z")] // invalid hex chars
    public void NormalizeHex_InvalidInputs_ReturnsNull(string? input)
    {
        var result = ProfileValidationUtilities.NormalizeHex(input!);
        Assert.Null(result);
    }

    // TryValidateTimezone
    [Theory]
    [InlineData("UTC", true)]
    [InlineData("America/New_York", true)]
    [InlineData("Europe/London", true)]
    [InlineData("Asia/Tokyo", true)]
    [InlineData("Europe/Aarhus", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void TryValidateTimezone_WorksAsExpected(string? input, bool expected)
    {
        var result = ProfileValidationUtilities.TryValidateTimezone(input!);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void TryValidateTimezone_WithLocalTimeZone_ReturnsTrue()
    {
        var localId = TimeZoneInfo.Local.Id; // always valid on the running system
        var result = ProfileValidationUtilities.TryValidateTimezone(localId);
        Assert.True(result);
    }

    [Fact]
    public void TryValidateTimezone_WithInvalidId_ReturnsFalse()
    {
        var result = ProfileValidationUtilities.TryValidateTimezone("Totally/Invalid_TimeZone_ID_!!");
        Assert.False(result);
    }

    // TryParseBirthday
    [Theory]
    [InlineData("07-21", "2000-07-21")]
    [InlineData("7-21", "2000-07-21")]
    [InlineData("7/21", "2000-07-21")]
    [InlineData(" 07/21 ", "2000-07-21")]
    [InlineData("02-29", "2000-02-29")] // leap day valid in year 2000
    [InlineData("2--3", "2000-02-03")] // consecutive separators collapse due to RemoveEmptyEntries
    public void TryParseBirthday_ValidInputs_ReturnsTrueAndNormalized(string input, string expected)
    {
        var ok = ProfileValidationUtilities.TryParseBirthday(input, out var stored);
        Assert.True(ok);
        Assert.Equal(expected, stored);
    }

    [Theory]
    [InlineData("13-01")] // invalid month
    [InlineData("00-10")] // invalid month
    [InlineData("02-30")] // invalid day
    [InlineData("2-31")] // invalid day
    [InlineData("abc")] // not numbers
    [InlineData("7-")] // missing part
    [InlineData("")] // empty
    [InlineData(null)] // null
    public void TryParseBirthday_InvalidInputs_ReturnsFalseAndEmpty(string? input)
    {
        var ok = ProfileValidationUtilities.TryParseBirthday(input!, out var stored);
        Assert.False(ok);
        Assert.True(string.IsNullOrEmpty(stored));
    }
}