using System.Runtime.Serialization;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using dotBento.Infrastructure.Services;
using dotBento.Infrastructure.Utilities;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

public enum LastFmTimePeriodChoice
{
    [SlashCommandChoice(Name = "Overall")] Overall,
    [SlashCommandChoice(Name = "7 Days")] SevenDays,
    [SlashCommandChoice(Name = "1 Month")] OneMonth,
    [SlashCommandChoice(Name = "3 Months")] ThreeMonths,
    [SlashCommandChoice(Name = "6 Months")] SixMonths,
    [SlashCommandChoice(Name = "1 Year")] OneYear
}

public enum LastFmCollageType
{
    [SlashCommandChoice(Name = "Top Artists")] TopArtists,
    [SlashCommandChoice(Name = "Top Albums")] TopAlbums,
    [SlashCommandChoice(Name = "Top Tracks")] TopTracks
}

public enum LastFmCollageSize
{
    [SlashCommandChoice(Name = "1x1")] OneByOne,
    [SlashCommandChoice(Name = "2x2")] TwoByTwo,
    [SlashCommandChoice(Name = "3x3")] ThreeByThree,
    [SlashCommandChoice(Name = "4x4")] FourByFour,
    [SlashCommandChoice(Name = "5x5")] FiveByFive,
    [SlashCommandChoice(Name = "6x6")] SixBySix
}

[SlashCommand("lastfm", "Commands for Discord Users")]
public sealed class LastFmSlashCommand(InteractiveService interactiveService, LastFmCommand lastFmCommand, UserSettingService userSettingService)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    [SubSlashCommand("nowplaying", "Show what you're currently listening to")]
    public async Task NowPlayingCommand(
        [SlashCommandParameter(Name = "user", Description = "For a user who has saved lastfm")]
        User? user = null,
        [SlashCommandParameter(Name = "hide", Description = "Only show user info for you")]
        bool? hide = null)
    {
        user ??= Context.User;
        var guildUser = Context.Guild?.Users.GetValueOrDefault(user.Id);
        var username = guildUser?.Nickname ?? guildUser?.GlobalName ?? user.GlobalName;
        var userAvatar = guildUser?.GetGuildAvatarUrl()?.ToString(1024) ?? user.GetAvatarUrl()?.ToString(1024);
        await Context.SendResponse(interactiveService,
            await lastFmCommand.GetNowPlaying((long)user.Id,
                username,
                userAvatar),
            hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
    }

    [SubSlashCommand("topartists", "Show top artists for a user")]
    public async Task TopArtistsCommand(
        [SlashCommandParameter(Name = "time-period", Description = "Time period")]
        LastFmTimePeriodChoice timePeriodDisplay = LastFmTimePeriodChoice.Overall,
        [SlashCommandParameter(Name = "user", Description = "For a user who has saved lastfm")]
        User? user = null,
        [SlashCommandParameter(Name = "collage", Description = "Show a collage of your top artists")]
        bool? collage = false,
        [SlashCommandParameter(Name = "hide", Description = "Only show user info for you")]
        bool? hide = null)
    {
        await Context.DeferResponseAsync();
        user ??= Context.User;
        var guildUser = Context.Guild?.Users.GetValueOrDefault(user.Id);
        var username = guildUser?.Nickname ?? guildUser?.GlobalName ?? user.GlobalName;
        var userAvatar = guildUser?.GetGuildAvatarUrl()?.ToString(1024) ?? user.GetAvatarUrl()?.ToString(1024);
        var period = LastFmTimePeriodUtilities.LastFmTimeSpanFromUserOptionSlashCommand(TimePeriodToString(timePeriodDisplay));
        try
        {
            await Context.SendFollowUpResponse(interactiveService,
                collage ?? false
                    ? await lastFmCommand.GetTopArtistsCollage((long)user.Id,
                        userAvatar,
                        period,
                        "6x6")
                    : await lastFmCommand.GetTopArtists((long)user.Id,
                        username,
                        userAvatar,
                        period),
                hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
        }
        catch (Exception e)
        {
            await Context.HandleCommandException(e);
        }
    }

    [SubSlashCommand("topalbums", "Show top albums for a user")]
    public async Task TopAlbumsCommand(
        [SlashCommandParameter(Name = "time-period", Description = "Time period")]
        LastFmTimePeriodChoice timePeriodDisplay = LastFmTimePeriodChoice.Overall,
        [SlashCommandParameter(Name = "user", Description = "For a user who has saved lastfm")]
        User? user = null,
        [SlashCommandParameter(Name = "collage", Description = "Show a collage of your top artists")]
        bool? collage = false,
        [SlashCommandParameter(Name = "hide", Description = "Only show user info for you")]
        bool? hide = null)
    {
        await Context.DeferResponseAsync();
        user ??= Context.User;
        var guildUser = Context.Guild?.Users.GetValueOrDefault(user.Id);
        var username = guildUser?.Nickname ?? guildUser?.GlobalName ?? user.GlobalName;
        var userAvatar = guildUser?.GetGuildAvatarUrl()?.ToString(1024) ?? user.GetAvatarUrl()?.ToString(1024);
        var period = LastFmTimePeriodUtilities.LastFmTimeSpanFromUserOptionSlashCommand(TimePeriodToString(timePeriodDisplay));
        try
        {
            await Context.SendFollowUpResponse(interactiveService,
                collage ?? false
                    ? await lastFmCommand.GetTopAlbumsCollage((long)user.Id,
                        userAvatar,
                        period,
                        "6x6")
                    : await lastFmCommand.GetTopAlbums((long)user.Id,
                        username,
                        userAvatar,
                        period),
                hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
        }
        catch (Exception e)
        {
            await Context.HandleCommandException(e);
        }
    }

    [SubSlashCommand("toptracks", "Show top tracks for a user")]
    public async Task TopTracksCommand(
        [SlashCommandParameter(Name = "time-period", Description = "Time period")]
        LastFmTimePeriodChoice timePeriodDisplay = LastFmTimePeriodChoice.Overall,
        [SlashCommandParameter(Name = "user", Description = "For a user who has saved lastfm")]
        User? user = null,
        [SlashCommandParameter(Name = "collage", Description = "Show a collage of your top artists")]
        bool? collage = false,
        [SlashCommandParameter(Name = "hide", Description = "Only show user info for you")]
        bool? hide = null)
    {
        await Context.DeferResponseAsync();
        user ??= Context.User;
        var guildUser = Context.Guild?.Users.GetValueOrDefault(user.Id);
        var username = guildUser?.Nickname ?? guildUser?.GlobalName ?? user.GlobalName;
        var userAvatar = guildUser?.GetGuildAvatarUrl()?.ToString(1024) ?? user.GetAvatarUrl()?.ToString(1024);
        var period = LastFmTimePeriodUtilities.LastFmTimeSpanFromUserOptionSlashCommand(TimePeriodToString(timePeriodDisplay));
        try
        {
            await Context.SendFollowUpResponse(interactiveService,
                collage ?? false
                    ? await lastFmCommand.GetTopTracksCollage((long)user.Id,
                        userAvatar,
                        period,
                        "6x6")
                    : await lastFmCommand.GetTopTracks((long)user.Id,
                        username,
                        userAvatar,
                        period),
                hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
        }
        catch (Exception e)
        {
            await Context.HandleCommandException(e);
        }
    }

    [SubSlashCommand("user", "Show user info for a user")]
    public async Task UserCommand(
        [SlashCommandParameter(Name = "user", Description = "For a user who has saved lastfm")]
        User? user = null,
        [SlashCommandParameter(Name = "hide", Description = "Only show user info for you")]
        bool? hide = null)
    {
        user ??= Context.User;
        var guildUser = Context.Guild?.Users.GetValueOrDefault(user.Id);
        var username = guildUser?.Nickname ?? guildUser?.GlobalName ?? user.GlobalName;
        var userAvatar = guildUser?.GetGuildAvatarUrl()?.ToString(1024) ?? user.GetAvatarUrl()?.ToString(1024);
        await Context.SendResponse(interactiveService,
            await lastFmCommand.GetUserInfo((long)user.Id, username, userAvatar), hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
    }

    [SubSlashCommand("recenttracks", "Show user info for a user")]
    public async Task RecentTracksCommand(
        [SlashCommandParameter(Name = "user", Description = "For a user who has saved lastfm")]
        User? user = null,
        [SlashCommandParameter(Name = "hide", Description = "Only show user info for you")]
        bool? hide = null)
    {
        user ??= Context.User;
        var guildUser = Context.Guild?.Users.GetValueOrDefault(user.Id);
        var username = guildUser?.Nickname ?? guildUser?.GlobalName ?? user.GlobalName;
        var userAvatar = guildUser?.GetGuildAvatarUrl()?.ToString(1024) ?? user.GetAvatarUrl()?.ToString(1024);
        await Context.SendResponse(interactiveService,
            await lastFmCommand.GetRecentTracks((long)user.Id, username, userAvatar), hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
    }

    [SubSlashCommand("save", "Set your lastfm username")]
    public async Task SaveLastFmCommand(
        [SlashCommandParameter(Name = "username", Description = "For a user who has saved lastfm")]
        string username) =>
        await Context.SendResponse(interactiveService,
            await lastFmCommand.SaveLastFmUser((long)Context.User.Id, username), true);

    [SubSlashCommand("delete", "Delete your lastfm username")]
    public async Task DeleteLastFmCommand() =>
        await Context.SendResponse(interactiveService, await lastFmCommand.DeleteLastFmUser((long)Context.User.Id),
            true);

    [SubSlashCommand("collage", "Generate a collage of your top artists, albums, or tracks")]
    public async Task CollageCommand(
        [SlashCommandParameter(Name = "type", Description = "Type of collage")]
        LastFmCollageType type = LastFmCollageType.TopArtists,
        [SlashCommandParameter(Name = "time-period", Description = "Time period")]
        LastFmTimePeriodChoice timePeriodDisplay = LastFmTimePeriodChoice.Overall,
        [SlashCommandParameter(Name = "images", Description = "Number of images to display")]
        LastFmCollageSize imageOption = LastFmCollageSize.FourByFour,
        [SlashCommandParameter(Name = "user", Description = "For a user who has saved lastfm")]
        User? user = null,
        [SlashCommandParameter(Name = "hide", Description = "Only show user info for you")]
        bool? hide = null)
    {
        await Context.DeferResponseAsync();
        user ??= Context.User;
        var guildUser = Context.Guild?.Users.GetValueOrDefault(user.Id);
        var userAvatar = guildUser?.GetGuildAvatarUrl()?.ToString(1024) ?? user.GetAvatarUrl()?.ToString(1024);
        var period = LastFmTimePeriodUtilities.LastFmTimeSpanFromUserOptionSlashCommand(TimePeriodToString(timePeriodDisplay));
        var sizeStr = CollageSizeToString(imageOption);
        var collage = type switch
        {
            LastFmCollageType.TopArtists => await lastFmCommand.GetTopArtistsCollage((long)user.Id, userAvatar, period, sizeStr),
            LastFmCollageType.TopAlbums => await lastFmCommand.GetTopAlbumsCollage((long)user.Id, userAvatar, period, sizeStr),
            LastFmCollageType.TopTracks => await lastFmCommand.GetTopTracksCollage((long)user.Id, userAvatar, period, sizeStr),
            _ => throw new SerializationException("Invalid type for lastfm collage")
        };
        try
        {
            await Context.SendFollowUpResponse(interactiveService, collage, hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
        }
        catch (Exception e)
        {
            await Context.HandleCommandException(e);
        }
    }

    private static string TimePeriodToString(LastFmTimePeriodChoice period) => period switch
    {
        LastFmTimePeriodChoice.SevenDays => "7 Days",
        LastFmTimePeriodChoice.OneMonth => "1 Month",
        LastFmTimePeriodChoice.ThreeMonths => "3 Months",
        LastFmTimePeriodChoice.SixMonths => "6 Months",
        LastFmTimePeriodChoice.OneYear => "1 Year",
        _ => "Overall"
    };

    private static string CollageSizeToString(LastFmCollageSize size) => size switch
    {
        LastFmCollageSize.OneByOne => "1x1",
        LastFmCollageSize.TwoByTwo => "2x2",
        LastFmCollageSize.ThreeByThree => "3x3",
        LastFmCollageSize.FiveByFive => "5x5",
        LastFmCollageSize.SixBySix => "6x6",
        _ => "4x4"
    };
}
