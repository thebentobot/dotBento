using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.AutoCompleteHandlers;
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
        [Summary("user", "For a user who has saved lastfm")] SocketUser? user = null,
        [Summary("hide", "Only show user info for you")] bool? hide = false)
    {
        user ??= Context.User;
        var username = Context.Guild is null ? user.GlobalName : Context.Guild.Users.First(x => x.Id == user.Id).Nickname ?? user.GlobalName;
        var userAvatar = Context.Guild is null ? user.GetAvatarUrl() : Context.Guild.Users.First(x => x.Id == user.Id).GetGuildAvatarUrl() ?? user.GetDisplayAvatarUrl();
        await Context.SendResponse(interactiveService, await lastFmCommand.GetNowPlaying((long)user.Id, username, userAvatar), hide ?? false);
    }
    
    [SlashCommand("topartists", "Show top artists for a user")]
    public async Task TopArtistsCommand(
        [Summary("timePeriod", "Time period")][Autocomplete(typeof(DateTimeAutoComplete))] string? timePeriodDisplay = null,
        [Summary("user", "For a user who has saved lastfm")] SocketUser? user = null,
        [Summary("hide", "Only show user info for you")] bool? hide = false)
    {
        user ??= Context.User;
        var username = Context.Guild is null ? user.GlobalName : Context.Guild.Users.First(x => x.Id == user.Id).Nickname ?? user.GlobalName;
        var userAvatar = Context.Guild is null ? user.GetAvatarUrl() : Context.Guild.Users.First(x => x.Id == user.Id).GetGuildAvatarUrl() ?? user.GetDisplayAvatarUrl();
        var period = LastFmTimePeriodUtilities.LastFmTimeSpanFromUserOptionSlashCommand(timePeriodDisplay ?? "Overall");
        await Context.SendResponse(interactiveService, await lastFmCommand.GetTopArtists((long)user.Id, username, userAvatar, period), hide ?? false);
    }
    
    [SlashCommand("topalbums", "Show top albums for a user")]
    public async Task TopAlbumsCommand(
        [Summary("timePeriod", "Time period")][Autocomplete(typeof(DateTimeAutoComplete))] string? timePeriodDisplay = null,
        [Summary("user", "For a user who has saved lastfm")] SocketUser? user = null,
        [Summary("hide", "Only show user info for you")] bool? hide = false)
    {
        user ??= Context.User;
        var username = Context.Guild is null ? user.GlobalName : Context.Guild.Users.First(x => x.Id == user.Id).Nickname ?? user.GlobalName;
        var userAvatar = Context.Guild is null ? user.GetAvatarUrl() : Context.Guild.Users.First(x => x.Id == user.Id).GetGuildAvatarUrl() ?? user.GetDisplayAvatarUrl();
        var period = LastFmTimePeriodUtilities.LastFmTimeSpanFromUserOptionSlashCommand(timePeriodDisplay ?? "Overall");
        await Context.SendResponse(interactiveService, await lastFmCommand.GetTopAlbums((long)user.Id, username, userAvatar, period), hide ?? false);
    }
    
    [SlashCommand("toptracks", "Show top tracks for a user")]
    public async Task TopTracksCommand(
        [Summary("timePeriod", "Time period")][Autocomplete(typeof(DateTimeAutoComplete))] string? timePeriodDisplay = null,
        [Summary("user", "For a user who has saved lastfm")] SocketUser? user = null,
        [Summary("hide", "Only show user info for you")] bool? hide = false)
    {
        user ??= Context.User;
        var username = Context.Guild is null ? user.GlobalName : Context.Guild.Users.First(x => x.Id == user.Id).Nickname ?? user.GlobalName;
        var userAvatar = Context.Guild is null ? user.GetAvatarUrl() : Context.Guild.Users.First(x => x.Id == user.Id).GetGuildAvatarUrl() ?? user.GetDisplayAvatarUrl();
        var period = LastFmTimePeriodUtilities.LastFmTimeSpanFromUserOptionSlashCommand(timePeriodDisplay ?? "Overall");
        await Context.SendResponse(interactiveService, await lastFmCommand.GetTopTracks((long)user.Id, username, userAvatar, period), hide ?? false);
    }
    
    [SlashCommand("user", "Show user info for a user")]
    public async Task TopTracksCommand(
        [Summary("user", "For a user who has saved lastfm")] SocketUser? user = null,
        [Summary("hide", "Only show user info for you")] bool? hide = false)
    {
        user ??= Context.User;
        var username = Context.Guild is null ? user.GlobalName : Context.Guild.Users.First(x => x.Id == user.Id).Nickname ?? user.GlobalName;
        var userAvatar = Context.Guild is null ? user.GetAvatarUrl() : Context.Guild.Users.First(x => x.Id == user.Id).GetGuildAvatarUrl() ?? user.GetDisplayAvatarUrl();
        await Context.SendResponse(interactiveService, await lastFmCommand.GetUserInfo((long)user.Id, username, userAvatar), hide ?? false);
    }
    
    [SlashCommand("recenttracks", "Show user info for a user")]
    public async Task RecentTracksCommand(
        [Summary("user", "For a user who has saved lastfm")] SocketUser? user = null,
        [Summary("hide", "Only show user info for you")] bool? hide = false)
    {
        user ??= Context.User;
        var username = Context.Guild is null ? user.GlobalName : Context.Guild.Users.First(x => x.Id == user.Id).Nickname ?? user.GlobalName;
        var userAvatar = Context.Guild is null ? user.GetAvatarUrl() : Context.Guild.Users.First(x => x.Id == user.Id).GetGuildAvatarUrl() ?? user.GetDisplayAvatarUrl();
        await Context.SendResponse(interactiveService, await lastFmCommand.GetRecentTracks((long)user.Id, username, userAvatar), hide ?? false);
    }
    
    [SlashCommand("save", "Set your lastfm username")]
    public async Task SaveLastFmCommand(
        [Summary("username", "For a user who has saved lastfm")] string username) =>
        await Context.SendResponse(interactiveService, await lastFmCommand.SaveLastFmUser((long)Context.User.Id, username), true);
    
    [SlashCommand("delete", "Delete your lastfm username")]
    public async Task DeleteLastFmCommand() =>
        await Context.SendResponse(interactiveService, await lastFmCommand.DeleteLastFmUser((long)Context.User.Id), true);
}