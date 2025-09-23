using dotBento.Bot.Utilities;

namespace dotBento.Bot.Tests.Utilities;

public class RegexPatternsTests
{
    [Theory]
    [InlineData("<:smile:123456>")]
    [InlineData("<a:wave:987654>")]
    [InlineData("Hello <:hiya:42> world")]
    [InlineData("Multi\nLine\n<a:dance:555>")]
    public void HasEmoteRegex_Matches_WhenMessageContainsAnyEmote(string input)
    {
        Assert.Matches(RegexPatterns.HasEmoteRegex, input);
    }

    [Theory]
    [InlineData("")]
    [InlineData(":smile:123456>")]
    [InlineData("<:smile:>")]
    [InlineData("<:smile:abc>")]
    [InlineData("not an emote")]
    public void HasEmoteRegex_DoesNotMatch_InvalidOrNoEmote(string input)
    {
        Assert.DoesNotMatch(RegexPatterns.HasEmoteRegex, input);
    }

    [Theory]
    [InlineData("<:smile:123456>", "123456")]
    [InlineData("text before <:hiya:42> text after", "42")]
    [InlineData("multi\nline <:ok:777> here", "777")]
    public void EmoteRegex_Matches_StaticEmote_AndCapturesId(string input, string expectedId)
    {
        var match = RegexPatterns.EmoteRegex.Match(input);
        Assert.True(match.Success);
        Assert.Equal(expectedId, match.Groups[1].Value);
    }

    [Theory]
    [InlineData("<a:wave:987654>")]
    [InlineData("text <a:dance:555> text")]
    public void EmoteRegex_DoesNotMatch_AnimatedEmote(string input)
    {
        var match = RegexPatterns.EmoteRegex.Match(input);
        Assert.False(match.Success);
    }

    [Theory]
    [InlineData("<a:wave:987654>", "987654")]
    [InlineData("prefix <a:dance:555> suffix", "555")]
    public void AnimatedEmoteRegex_Matches_AnimatedEmote_AndCapturesId(string input, string expectedId)
    {
        var match = RegexPatterns.AnimatedEmoteRegex.Match(input);
        Assert.True(match.Success);
        Assert.Equal(expectedId, match.Groups[1].Value);
    }

    [Theory]
    [InlineData("<:smile:123456>")]
    [InlineData("text <:ok:777> more")]
    public void AnimatedEmoteRegex_DoesNotMatch_StaticEmote(string input)
    {
        var match = RegexPatterns.AnimatedEmoteRegex.Match(input);
        Assert.False(match.Success);
    }
}