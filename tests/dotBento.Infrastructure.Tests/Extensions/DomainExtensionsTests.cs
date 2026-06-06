using dotBento.Domain.Enums.Games;
using dotBento.Domain.Extensions;
using dotBento.Domain.Extensions.Games;

namespace dotBento.Infrastructure.Tests.Extensions;

public sealed class DomainExtensionsTests
{
    [Fact]
    public void SplitByMessageLength_SplitsIntoDiscordSizedChunks()
    {
        var value = string.Concat(Enumerable.Repeat("a", 4_050));

        var chunks = value.SplitByMessageLength().ToList();

        Assert.Equal([2_000, 2_000, 50], chunks.Select(chunk => chunk.Length));
        Assert.Equal(value, string.Concat(chunks));
    }

    [Theory]
    [InlineData("@everyone hello", " hello")]
    [InlineData("@here hello", " hello")]
    [InlineData("<@123> hello", "123> hello")]
    [InlineData("`code`", "code")]
    public void FilterOutMentions_RemovesMentionTriggersAndBackticks(string input, string expected)
    {
        Assert.Equal(expected, input.FilterOutMentions());
    }

    [Theory]
    [InlineData("plain text", false)]
    [InlineData("hello_world", true)]
    [InlineData("hello@world", true)]
    [InlineData("path/to/file", true)]
    public void ContainsSensitiveCharacters_DetectsMarkdownAndMentionCharacters(string input, bool expected)
    {
        Assert.Equal(expected, input.ContainsSensitiveCharacters());
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("abc", false)]
    [InlineData("smile 😊", true)]
    public void ContainsUnicodeCharacter_DetectsCharactersOutsideAnsiRange(string? input, bool expected)
    {
        Assert.Equal(expected, input!.ContainsUnicodeCharacter());
    }

    [Fact]
    public void ReplaceInvalidChars_ReplacesKnownFilenameSeparators()
    {
        var result = "a \"quoted\" file.name".ReplaceInvalidChars();

        Assert.Equal("a__quoted__file_name", result);
    }

    [Fact]
    public void Sanitize_EscapesSensitiveCharacters()
    {
        var result = @"\*_~`:/ >|#@".Sanitize();

        Assert.Equal(@"\\\*\_\~\`\:\/ \>\|\#\@", result);
    }

    [Theory]
    [InlineData(null, 3, null)]
    [InlineData("", 3, "")]
    [InlineData("abc", 10, "abc")]
    [InlineData("abcdef", 3, "abc")]
    public void TruncateLongString_TruncatesOnlyWhenNeeded(string? input, int maxLength, string? expected)
    {
        Assert.Equal(expected, input!.TruncateLongString(maxLength));
    }

    [Theory]
    [InlineData(null, 5, null)]
    [InlineData("", 5, "")]
    [InlineData("abc", 5, "abc")]
    [InlineData("abcdef", 5, "ab...")]
    public void TrimToMaxLength_AddsEllipsisWhenTrimming(string? input, int maxLength, string? expected)
    {
        Assert.Equal(expected, input!.TrimToMaxLength(maxLength));
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("bento", "Bento")]
    [InlineData("Bento", "Bento")]
    public void CapitalizeFirstLetter_HandlesNullEmptyAndText(string? input, string expected)
    {
        Assert.Equal(expected, input.CapitalizeFirstLetter());
    }

    [Fact]
    public void ReplaceOrAddToList_ReplacesCaseVariantAndAddsMissingOptions()
    {
        var list = new List<string> { "One", "Two" };

        list.ReplaceOrAddToList(["one", "THREE", "Two"]);

        Assert.Equal(["one", "Two", "THREE"], list);
    }

    [Theory]
    [InlineData(RpsGameChoice.Rock, "Rock 🪨")]
    [InlineData(RpsGameChoice.Paper, "Paper 📄")]
    [InlineData(RpsGameChoice.Scissors, "Scissors ✂️")]
    public void AddEmoji_FormatsRpsChoice(RpsGameChoice choice, string expected)
    {
        Assert.Equal(expected, choice.AddEmoji());
    }

    [Fact]
    public void AddEmoji_ThrowsForUnknownRpsChoice()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((RpsGameChoice)999).AddEmoji());
    }

    [Theory]
    [InlineData(RpsGameResult.Win, "You **Win** 🎉")]
    [InlineData(RpsGameResult.Loss, "You **Lose** 💀")]
    [InlineData(RpsGameResult.Draw, "It's a **Draw** 🤝")]
    public void FormatResult_FormatsRpsResult(RpsGameResult result, string expected)
    {
        Assert.Equal(expected, result.FormatResult());
    }

    [Fact]
    public void FormatResult_ThrowsForUnknownRpsResult()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((RpsGameResult)999).FormatResult());
    }
}
