using System.Runtime.Serialization;
using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using dotBento.Infrastructure.Utilities;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

[Group("lastfm", "Commands for Discord Users")]
public class LastFmSlashCommand(InteractiveService interactiveService, LastFmCommand lastFmCommand)
    : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("nowplaying", "Show what you're currently listening to")]
    public async Task NowPlayingCommand(
        [Summary("user", "For a user who has saved lastfm")]
        SocketUser? user = null,
        [Summary("hide", "Only show user info for you")]
        bool? hide = false)
    {
        user ??= Context.User;
        var username = Context.Guild is null
            ? user.GlobalName
            : Context.Guild.Users.First(x => x.Id == user.Id)
                  .Nickname ??
              user.GlobalName;
        var userAvatar = Context.Guild is null
            ? user.GetAvatarUrl()
            : Context.Guild.Users.First(x => x.Id == user.Id)
                  .GetGuildAvatarUrl() ??
              user.GetDisplayAvatarUrl();
        await Context.SendResponse(interactiveService,
            await lastFmCommand.GetNowPlaying((long)user.Id,
                username,
                userAvatar),
            hide ?? false);
    }

    [SlashCommand("topartists", "Show top artists for a user")]
    public async Task TopArtistsCommand(
        [Summary("timePeriod", "Time period")]
        [Choice("Overall", "Overall")]
        [Choice("7 Days", "7 Days")]
        [Choice("1 Month", "1 Month")]
        [Choice("3 Months", "3 Months")]
        [Choice("6 Months", "6 Months")]
        [Choice("1 Year", "1 Year")]
        string? timePeriodDisplay = "Overall",
        [Summary("user", "For a user who has saved lastfm")]
        SocketUser? user = null,
        [Summary("collage", "Show a collage of your top artists")]
        bool? collage = false,
        [Summary("hide", "Only show user info for you")]
        bool? hide = false)
    {
        _ = DeferAsync();
        user ??= Context.User;
        var username = Context.Guild is null
            ? user.GlobalName
            : Context.Guild.Users.First(x => x.Id == user.Id).Nickname ?? user.GlobalName;
        var userAvatar = Context.Guild is null
            ? user.GetAvatarUrl()
            : Context.Guild.Users.First(x => x.Id == user.Id).GetGuildAvatarUrl() ?? user.GetDisplayAvatarUrl();
        var period = LastFmTimePeriodUtilities.LastFmTimeSpanFromUserOptionSlashCommand(timePeriodDisplay ?? "Overall");
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
                hide ?? false);
        }
        catch (Exception e)
        {
            await Context.HandleCommandException(e);
        }
    }

    [SlashCommand("topalbums", "Show top albums for a user")]
    public async Task TopAlbumsCommand(
        [Summary("timePeriod", "Time period")]
        [Choice("Overall", "Overall")]
        [Choice("7 Days", "7 Days")]
        [Choice("1 Month", "1 Month")]
        [Choice("3 Months", "3 Months")]
        [Choice("6 Months", "6 Months")]
        [Choice("1 Year", "1 Year")]
        string? timePeriodDisplay = "Overall",
        [Summary("user", "For a user who has saved lastfm")]
        SocketUser? user = null,
        [Summary("collage", "Show a collage of your top artists")]
        bool? collage = false,
        [Summary("hide", "Only show user info for you")]
        bool? hide = false)
    {
        _ = DeferAsync();
        user ??= Context.User;
        var username = Context.Guild is null
            ? user.GlobalName
            : Context.Guild.Users.First(x => x.Id == user.Id).Nickname ?? user.GlobalName;
        var userAvatar = Context.Guild is null
            ? user.GetAvatarUrl()
            : Context.Guild.Users.First(x => x.Id == user.Id).GetGuildAvatarUrl() ?? user.GetDisplayAvatarUrl();
        var period = LastFmTimePeriodUtilities.LastFmTimeSpanFromUserOptionSlashCommand(timePeriodDisplay ?? "Overall");
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
                hide ?? false);
        }
        catch (Exception e)
        {
            await Context.HandleCommandException(e);
        }
    }

    [SlashCommand("toptracks", "Show top tracks for a user")]
    public async Task TopTracksCommand(
        [Summary("timePeriod", "Time period")]
        [Choice("Overall", "Overall")]
        [Choice("7 Days", "7 Days")]
        [Choice("1 Month", "1 Month")]
        [Choice("3 Months", "3 Months")]
        [Choice("6 Months", "6 Months")]
        [Choice("1 Year", "1 Year")]
        string timePeriodDisplay = "Overall",
        [Summary("user", "For a user who has saved lastfm")]
        SocketUser? user = null,
        [Summary("collage", "Show a collage of your top artists")]
        bool? collage = false,
        [Summary("hide", "Only show user info for you")]
        bool? hide = false)
    {
        _ = DeferAsync();
        user ??= Context.User;
        var username = Context.Guild is null
            ? user.GlobalName
            : Context.Guild.Users.First(x => x.Id == user.Id).Nickname ?? user.GlobalName;
        var userAvatar = Context.Guild is null
            ? user.GetAvatarUrl()
            : Context.Guild.Users.First(x => x.Id == user.Id).GetGuildAvatarUrl() ?? user.GetDisplayAvatarUrl();
        var period = LastFmTimePeriodUtilities.LastFmTimeSpanFromUserOptionSlashCommand(timePeriodDisplay);
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
                hide ?? false);
        }
        catch (Exception e)
        {
            await Context.HandleCommandException(e);
        }
    }

    [SlashCommand("user", "Show user info for a user")]
    public async Task UserCommand(
        [Summary("user", "For a user who has saved lastfm")]
        SocketUser? user = null,
        [Summary("hide", "Only show user info for you")]
        bool? hide = false)
    {
        user ??= Context.User;
        var username = Context.Guild is null
            ? user.GlobalName
            : Context.Guild.Users.First(x => x.Id == user.Id).Nickname ?? user.GlobalName;
        var userAvatar = Context.Guild is null
            ? user.GetAvatarUrl()
            : Context.Guild.Users.First(x => x.Id == user.Id).GetGuildAvatarUrl() ?? user.GetDisplayAvatarUrl();
        await Context.SendResponse(interactiveService,
            await lastFmCommand.GetUserInfo((long)user.Id, username, userAvatar), hide ?? false);
    }

    [SlashCommand("recenttracks", "Show user info for a user")]
    public async Task RecentTracksCommand(
        [Summary("user", "For a user who has saved lastfm")]
        SocketUser? user = null,
        [Summary("hide", "Only show user info for you")]
        bool? hide = false)
    {
        user ??= Context.User;
        var username = Context.Guild is null
            ? user.GlobalName
            : Context.Guild.Users.First(x => x.Id == user.Id).Nickname ?? user.GlobalName;
        var userAvatar = Context.Guild is null
            ? user.GetAvatarUrl()
            : Context.Guild.Users.First(x => x.Id == user.Id).GetGuildAvatarUrl() ?? user.GetDisplayAvatarUrl();
        await Context.SendResponse(interactiveService,
            await lastFmCommand.GetRecentTracks((long)user.Id, username, userAvatar), hide ?? false);
    }

    [SlashCommand("save", "Set your lastfm username")]
    public async Task SaveLastFmCommand(
        [Summary("username", "For a user who has saved lastfm")]
        string username) =>
        await Context.SendResponse(interactiveService,
            await lastFmCommand.SaveLastFmUser((long)Context.User.Id, username), true);

    [SlashCommand("delete", "Delete your lastfm username")]
    public async Task DeleteLastFmCommand() =>
        await Context.SendResponse(interactiveService, await lastFmCommand.DeleteLastFmUser((long)Context.User.Id),
            true);

    [SlashCommand("collage", "Generate a collage of your top artists, albums, or tracks")]
    public async Task CollageCommand(
        [Summary("type", "Type of collage")]
        [Choice("Top Artists", "Top Artists")]
        [Choice("Top Albums", "Top Albums")]
        [Choice("Top Tracks", "Top Tracks")]
        string type = "Top Artists",
        [Summary("timePeriod", "Time period")]
        [Choice("Overall", "Overall")]
        [Choice("7 Days", "7 Days")]
        [Choice("1 Month", "1 Month")]
        [Choice("3 Months", "3 Months")]
        [Choice("6 Months", "6 Months")]
        [Choice("1 Year", "1 Year")]
        string timePeriodDisplay = "Overall",
        [Summary("images", "Number of images to display")]
        [Choice("1x1", "1x1")]
        [Choice("2x2", "2x2")]
        [Choice("3x3", "3x3")]
        [Choice("4x4", "4x4")]
        [Choice("5x5", "5x5")]
        [Choice("6x6", "6x6")]
        string? imageOption = "4x4",
        [Summary("user", "For a user who has saved lastfm")]
        SocketUser? user = null,
        [Summary("hide", "Only show user info for you")]
        bool? hide = false)
    {
        _ = DeferAsync();
        user ??= Context.User;
        var userAvatar = Context.Guild is null
            ? user.GetAvatarUrl()
            : Context.Guild.Users.First(x => x.Id == user.Id).GetGuildAvatarUrl() ?? user.GetDisplayAvatarUrl();
        var period = LastFmTimePeriodUtilities.LastFmTimeSpanFromUserOptionSlashCommand(timePeriodDisplay);
        var collage = type switch
        {
            "Top Artists" => await lastFmCommand.GetTopArtistsCollage((long)user.Id, userAvatar, period,
                imageOption ?? "4x4"),
            "Top Albums" => await lastFmCommand.GetTopAlbumsCollage((long)user.Id, userAvatar, period,
                imageOption ?? "4x4"),
            "Top Tracks" => await lastFmCommand.GetTopTracksCollage((long)user.Id, userAvatar, period,
                imageOption ?? "4x4"),
            _ => throw new SerializationException("Invalid type for lastfm collage")
        };
        try
        {
            await Context.SendFollowUpResponse(interactiveService, collage, hide ?? false);
        }
        catch (Exception e)
        {
            await Context.HandleCommandException(e);
        }
    }
}