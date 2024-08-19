using CSharpFunctionalExtensions;
using Discord;
using Discord.Commands;
using dotBento.Bot.Attributes;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using dotBento.Bot.Models.Discord;
using dotBento.Infrastructure.Utilities;
using Fergun.Interactive;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Commands.TextCommands;

[Name("LastFm")]
public class LastFmTextCommand(
    IOptions<BotEnvConfig> botSettings,
    InteractiveService interactiveService,
    LastFmCommand lastFmCommand) : BaseCommandModule(botSettings)
{

    [Command("lastfm", RunMode = RunMode.Async)]
    [Summary("LastFm commands to check what you're listening to, top artists, albums, tracks, etc.")]
    [Alias("fm")]
    [Examples("lastfm",
        "lastfm nowplaying",
        "fm np",
        "lastfm topartists week ",
        "fm ta half 223908083825377281",
        "lastfm topalbums month @Adam",
        "fm tal year",
        "lastfm toptracks quarter",
        "fm tt all",
        "lastfm recenttracks",
        "lastfm rt",
        "fm recent",
        "fm save charlixcxfan01",
        "fm delete",
        "fm user @Adam"
    )]
    [GuildOnly]
    public async Task LastFmCommand([Remainder] string? input = null)
    {
        _ = Context.Channel.TriggerTypingAsync();
    
        var args = input?.Split(' ') ?? [];
        var mentions = Context.Message.MentionedUsers;
            
        if (args.Length == 0)
        {
            var noArgGuildMember = Context.Guild.Users.Single(guildUser => guildUser.Id == Context.User.Id);
            await Context.SendResponse(interactiveService,
                await lastFmCommand.GetNowPlaying((long)Context.User.Id,
                    noArgGuildMember.Nickname ?? noArgGuildMember.GlobalName,
                    noArgGuildMember.GetGuildAvatarUrl() ?? noArgGuildMember.GetDisplayAvatarUrl()));
            return;
        }
            
        switch (args[0])
        {
            case "recent":
            case "recenttracks":
            case "rt":
                var getUserForUserCmd23 = Context.User;
                if (args.Length > 1)
                {
                    if (mentions.Count > 0)
                    {
                        getUserForUserCmd23 = mentions.First();
                    }
                    else
                    {
                        var tryGetUserForUserCmd = Context.Client.GetUser(Convert.ToUInt64(args[1]));
                        if (tryGetUserForUserCmd != null)
                        {
                            getUserForUserCmd23 = tryGetUserForUserCmd;
                        }
                        else
                        {
                            await Context.SendResponse(interactiveService, ErrorEmbed("The user you inserted is not in this server"));
                            return;
                        }
                    }
                }
                var userForUserCmd23 = Context.Guild.GetUser(getUserForUserCmd23.Id).AsMaybe();
                if (userForUserCmd23.HasNoValue)
                {
                    await Context.SendResponse(interactiveService, ErrorEmbed("The user you inserted is not in this server"));
                    return;
                }
                var guildMemberForUserCmd23 = userForUserCmd23.Value;
                var userAvatarForUserCmd23 = guildMemberForUserCmd23.GetGuildAvatarUrl() ?? guildMemberForUserCmd23.GetDisplayAvatarUrl();
                var usernameForUserCmd23 = guildMemberForUserCmd23.Nickname ?? guildMemberForUserCmd23.GlobalName;
                await Context.SendResponse(interactiveService, await lastFmCommand.GetRecentTracks((long)guildMemberForUserCmd23.Id, usernameForUserCmd23, userAvatarForUserCmd23));
                return;
            case "np":
            case "nowplaying":
                var getUserForUserCmd2 = Context.User;
                if (args.Length > 1)
                {
                    if (mentions.Count > 0)
                    {
                        getUserForUserCmd2 = mentions.First();
                    }
                    else
                    {
                        var tryGetUserForUserCmd = Context.Client.GetUser(Convert.ToUInt64(args[1]));
                        if (tryGetUserForUserCmd != null)
                        {
                            getUserForUserCmd2 = tryGetUserForUserCmd;
                        }
                        else
                        {
                            await Context.SendResponse(interactiveService, ErrorEmbed("The user you inserted is not in this server"));
                            return;
                        }
                    }
                }
                var userForUserCmd2 = Context.Guild.GetUser(getUserForUserCmd2.Id).AsMaybe();
                if (userForUserCmd2.HasNoValue)
                {
                    await Context.SendResponse(interactiveService, ErrorEmbed("The user you inserted is not in this server"));
                    return;
                }
                var guildMemberForUserCmd2 = userForUserCmd2.Value;
                var userAvatarForUserCmd2 = guildMemberForUserCmd2.GetGuildAvatarUrl() ?? guildMemberForUserCmd2.GetDisplayAvatarUrl();
                var usernameForUserCmd2 = guildMemberForUserCmd2.Nickname ?? guildMemberForUserCmd2.GlobalName;
                await Context.SendResponse(interactiveService, await lastFmCommand.GetNowPlaying((long)guildMemberForUserCmd2.Id, usernameForUserCmd2, userAvatarForUserCmd2));
                return;
            case "save":
                await Context.SendResponse(interactiveService, await lastFmCommand.SaveLastFmUser((long)Context.User.Id, args[1]));
                return;
            case "remove":
                await Context.SendResponse(interactiveService, await lastFmCommand.DeleteLastFmUser((long)Context.User.Id));
                return;
            case "user":
                var getUserForUserCmd = Context.User;
                if (args.Length > 1)
                {
                    if (mentions.Count > 0)
                    {
                        getUserForUserCmd = mentions.First();
                    }
                    else
                    {
                        var tryGetUserForUserCmd = Context.Client.GetUser(Convert.ToUInt64(args[1]));
                        if (tryGetUserForUserCmd != null)
                        {
                            getUserForUserCmd = tryGetUserForUserCmd;
                        }
                        else
                        {
                            await Context.SendResponse(interactiveService, ErrorEmbed("The user you inserted is not in this server"));
                            return;
                        }
                    }
                }
                var userForUserCmd = Context.Guild.GetUser(getUserForUserCmd.Id).AsMaybe();
                if (userForUserCmd.HasNoValue)
                {
                    await Context.SendResponse(interactiveService, ErrorEmbed("The user you inserted is not in this server"));
                    return;
                }
                var guildMemberForUserCmd = userForUserCmd.Value;
                var userAvatarForUserCmd = guildMemberForUserCmd.GetGuildAvatarUrl() ?? guildMemberForUserCmd.GetDisplayAvatarUrl();
                var usernameForUserCmd = guildMemberForUserCmd.Nickname ?? guildMemberForUserCmd.GlobalName;
                await Context.SendResponse(interactiveService, await lastFmCommand.GetUserInfo((long)guildMemberForUserCmd.Id, usernameForUserCmd, userAvatarForUserCmd));
                return;
        }

        var getUser = Context.User;
            
        var period = args.Length > 1 ? LastFmTimePeriodUtilities.LastFmTimeSpanFromUserOptionTextCommand(args[1]) : null;

        if (args.Length > 1 && period == null)
        {
            await Context.SendResponse(interactiveService, ErrorEmbed($"Your time period {args[1]} is not valid"));
            return;
        }

        if (args.Length > 2)
        {
            if (mentions.Count > 0)
            {
                getUser = mentions.First();
            }
            else
            {
                var tryGetUserForUserCmd = Context.Client.GetUser(Convert.ToUInt64(args[2]));
                if (tryGetUserForUserCmd != null)
                {
                    getUser = tryGetUserForUserCmd;
                }
                else
                {
                    await Context.SendResponse(interactiveService, ErrorEmbed("The user you inserted is not in this server"));
                    return;
                }
            }
        }

        var user = Context.Guild.GetUser(getUser.Id).AsMaybe();
        if (user.HasNoValue)
        {
            await Context.SendResponse(interactiveService, ErrorEmbed("The user you inserted is not in this server"));
            return;
        }
            
        var guildMember = user.Value;
    
        var userAvatar = guildMember.GetGuildAvatarUrl() ?? guildMember.GetDisplayAvatarUrl();
                
        var username = guildMember.Nickname ?? guildMember.GlobalName;

        switch (args.FirstOrDefault())
        {
            case "ta":
            case "topartists":
                await Context.SendResponse(interactiveService, await lastFmCommand.GetTopArtists((long)guildMember.Id, username, userAvatar, period ?? "Overall"));
                break;
            case "tal":
            case "topalbums":
                await Context.SendResponse(interactiveService, await lastFmCommand.GetTopAlbums((long)guildMember.Id, username, userAvatar, period ?? "Overall"));
                break;
            case "tt":
            case "toptracks":
                await Context.SendResponse(interactiveService, await lastFmCommand.GetTopTracks((long)guildMember.Id, username, userAvatar, period ?? "Overall"));
                break;
            default: 
                await Context.SendResponse(interactiveService,
                    await lastFmCommand.GetNowPlaying((long)Context.User.Id,
                        username,
                        userAvatar));
                break;
        }
    }

    private static ResponseModel ErrorEmbed(string error)
    {
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        embed.Embed.WithTitle(error)
            .WithColor(Color.Red);
        return embed;
    }
}