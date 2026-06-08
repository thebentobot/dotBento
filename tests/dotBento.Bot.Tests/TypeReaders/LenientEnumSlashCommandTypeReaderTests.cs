using dotBento.Bot.Commands.SlashCommands;
using dotBento.Bot.TypeReaders;
using dotBento.Domain.Enums.Games;
using NetCord.Services.ApplicationCommands;

namespace dotBento.Bot.Tests.TypeReaders;

public sealed class LenientEnumSlashCommandTypeReaderTests
{
    [Fact]
    public void TryReadEnum_AcceptsNumericValue()
    {
        var result = LenientEnumSlashCommandTypeReader<ApplicationCommandContext>.TryReadEnum("1", typeof(RpsGameChoice));

        Assert.Equal(RpsGameChoice.Paper, result);
    }

    [Fact]
    public void TryReadEnum_AcceptsEnumName()
    {
        var result = LenientEnumSlashCommandTypeReader<ApplicationCommandContext>.TryReadEnum("scissors", typeof(RpsGameChoice));

        Assert.Equal(RpsGameChoice.Scissors, result);
    }

    [Fact]
    public void TryReadEnum_AcceptsChoiceDisplayName()
    {
        var result = LenientEnumSlashCommandTypeReader<ApplicationCommandContext>.TryReadEnum("7 days", typeof(LastFmTimePeriodChoice));

        Assert.Equal(LastFmTimePeriodChoice.SevenDays, result);
    }

    [Fact]
    public void TryReadEnum_ReturnsNullForUnknownValue()
    {
        var result = LenientEnumSlashCommandTypeReader<ApplicationCommandContext>.TryReadEnum("lizard", typeof(RpsGameChoice));

        Assert.Null(result);
    }
}
