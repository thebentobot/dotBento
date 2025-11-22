using System.Reflection;
using dotBento.Infrastructure.Commands;

namespace dotBento.Infrastructure.Tests;

public class ProfileCommandsTests
{
    // Helper to invoke private static methods via reflection
    private static T? InvokePrivateStatic<T>(Type type, string methodName, params object?[]? args)
    {
        var method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);
        var result = method!.Invoke(null, args);
        return result is null ? default : (T)result;
    }

    [Theory]
    [InlineData("02-07", "Feb 7 🎂")]           // MM-dd
    [InlineData("2-7", "Feb 7 🎂")]             // M-d
    [InlineData("02/07", "Feb 7 🎂")]          // MM/dd
    [InlineData("2/7", "Feb 7 🎂")]            // M/d
    [InlineData("07-02", "Jul 2 🎂")]          // MM-dd (new syntax)
    [InlineData("7-2", "Jul 2 🎂")]            // M-d (new syntax)
    [InlineData("07/02", "Jul 2 🎂")]          // MM/dd (new syntax)
    [InlineData("7/2", "Jul 2 🎂")]            // M/d (new syntax)
    [InlineData("7 february", "Feb 7 🎂")]     // text, lowercase month
    [InlineData("February 18", "Feb 18 🎂")]   // text, Month d
    [InlineData("20 April 2000", "Apr 20 🎂")] // text with year
    [InlineData("25 Nov", "Nov 25 🎂")]        // short month
    [InlineData("  February   1  ", "Feb 1 🎂")] // extra spaces
    public void FormatBirthday_ValidInputs_ReturnsNormalized(string input, string expected)
    {
        var result = InvokePrivateStatic<string>(typeof(ProfileCommands), "FormatBirthday", input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a date")] // invalid
    public void FormatBirthday_InvalidOrEmpty_ReturnsEmpty(string? input)
    {
        var result = InvokePrivateStatic<string>(typeof(ProfileCommands), "FormatBirthday", input);
        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData(0, "🌌")]
    [InlineData(3, "🌌")]
    [InlineData(4, "🌅")]
    [InlineData(7, "🌅")]
    [InlineData(8, "☀️")]
    [InlineData(11, "☀️")]
    [InlineData(12, "🌞")]
    [InlineData(15, "🌞")]
    [InlineData(16, "🌇")]
    [InlineData(19, "🌇")]
    [InlineData(20, "🌙")]
    [InlineData(23, "🌙")]
    public void ShowEmoteAccordingToTimeOfDay_MapsHoursToEmotes(int hour, string expected)
    {
        var dt = new DateTime(2024, 1, 1, hour, 0, 0, DateTimeKind.Unspecified);
        var result = InvokePrivateStatic<string>(typeof(ProfileCommands), "ShowEmoteAccordingToTimeOfDay", dt);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(100, "FF")]
    [InlineData(0, "00")]
    [InlineData(50, "7F")] // 127.5 -> 127 -> 0x7F
    [InlineData(null, "FF")]
    public void ConvertOpacityToHex_WorksAsExpected(int? percent, string expectedHex)
    {
        var result = InvokePrivateStatic<string>(typeof(ProfileCommands), "ConvertOpacityToHex", percent);
        Assert.Equal(expectedHex, result);
    }

    [Theory]
    [InlineData("ShortName", "24px")]           // <= 15
    [InlineData("ThisIsEighteenLong", "18px")] // length 18 => <= 20
    [InlineData("abcdefghijklmnopqrstuv", "15px")] // length 22 => <= 25
    [InlineData("ThisUserNameIsWayTooLongForLargeFont", "11px")] // > 25
    public void UsernamePxSize_ScalesByLength(string username, string expectedPx)
    {
        var result = InvokePrivateStatic<string>(typeof(ProfileCommands), "UsernamePxSize", username);
        Assert.Equal(expectedPx, result);
    }

    [Theory]
    [InlineData(10, "16px")]
    [InlineData(38, "14px")]
    [InlineData(44, "12px")]
    [InlineData(60, "10px")]
    public void LastFmTextPxSize_ScalesByLength(int length, string expectedPx)
    {
        var result = InvokePrivateStatic<string>(typeof(ProfileCommands), "LastFmTextPxSize", length);
        Assert.Equal(expectedPx, result);
    }
}
