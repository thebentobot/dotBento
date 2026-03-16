using CSharpFunctionalExtensions;
using NetCord;
using NetCord.Gateway;
using NetCord.Services.Commands;
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

[ModuleName("LastFm")]
public sealed class LastFmTextCommand(
    IOptions<BotEnvConfig> botSettings,
    InteractiveService interactiveService,
    LastFmCommand lastFmCommand) : BaseCommandModule(botSettings)
{

    [Command("lastfm", "fm")]
    [Summary("LastFm commands to check what you're listening to, top artists, albums, tracks, etc.")]
    [Examples("lastfm",
        "lastfm nowplaying",
        "fm np",
        "lastfm topartists week",
        "fm ta half 223908083825377281",
        "lastfm topalbums month @Adam",
        "fm tal year",
        "lastfm toptracks quarter",
        "fm tt all",
        "lastfm recenttracks",
        "lastfm rt",
        "fm recent",
        "lastfm collage all @Lewis 2x2 topartists",
        "fm save charlixcxfan01",
        "fm delete",
        "fm user @Adam"
    )]
    [GuildOnly]
    public async Task LastFmCommand([CommandParameter(Remainder = true)] string? input = null)
    {
        _ = Context.Channel?.TriggerTypingStateAsync();

        var args = input?.Split(' ') ?? [];
        var mentions = Context.Message.MentionedUsers;

        if (args.Length == 0)
        {
            var noArgGuildMember = Context.Guild?.Users.GetValueOrDefault(Context.User.Id);
            await Context.SendResponse(interactiveService,
                await lastFmCommand.GetNowPlaying((long)Context.User.Id,
                    noArgGuildMember?.Nickname ?? noArgGuildMember?.GlobalName,
                    noArgGuildMember?.GetGuildAvatarUrl()?.ToString(1024) ?? noArgGuildMember?.GetAvatarUrl()?.ToString(1024)));
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
                        if (ulong.TryParse(args[1], out var parsedId23))
                        {
                            var tryGetUserForUserCmd = Context.Guild?.Users.GetValueOrDefault(parsedId23);
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
                        else
                        {
                            await Context.SendResponse(interactiveService, ErrorEmbed("The user you inserted is not in this server"));
                            return;
                        }
                    }
                }
                var guildMemberForUserCmd23 = Context.Guild?.Users.GetValueOrDefault(getUserForUserCmd23.Id).AsMaybe() ?? Maybe<GuildUser>.None;
                if (guildMemberForUserCmd23.HasNoValue)
                {
                    await Context.SendResponse(interactiveService, ErrorEmbed("The user you inserted is not in this server"));
                    return;
                }
                var memberForUserCmd23 = guildMemberForUserCmd23.Value;
                var userAvatarForUserCmd23 = memberForUserCmd23.GetGuildAvatarUrl()?.ToString(1024) ?? memberForUserCmd23.GetAvatarUrl()?.ToString(1024);
                var usernameForUserCmd23 = memberForUserCmd23.Nickname ?? memberForUserCmd23.GlobalName;
                await Context.SendResponse(interactiveService, await lastFmCommand.GetRecentTracks((long)memberForUserCmd23.Id, usernameForUserCmd23, userAvatarForUserCmd23));
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
                        if (ulong.TryParse(args[1], out var parsedId2))
                        {
                            var tryGetUserForUserCmd = Context.Guild?.Users.GetValueOrDefault(parsedId2);
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
                        else
                        {
                            await Context.SendResponse(interactiveService, ErrorEmbed("The user you inserted is not in this server"));
                            return;
                        }
                    }
                }
                var guildMemberForUserCmd2 = Context.Guild?.Users.GetValueOrDefault(getUserForUserCmd2.Id).AsMaybe() ?? Maybe<GuildUser>.None;
                if (guildMemberForUserCmd2.HasNoValue)
                {
                    await Context.SendResponse(interactiveService, ErrorEmbed("The user you inserted is not in this server"));
                    return;
                }
                var memberForUserCmd2 = guildMemberForUserCmd2.Value;
                var userAvatarForUserCmd2 = memberForUserCmd2.GetGuildAvatarUrl()?.ToString(1024) ?? memberForUserCmd2.GetAvatarUrl()?.ToString(1024);
                var usernameForUserCmd2 = memberForUserCmd2.Nickname ?? memberForUserCmd2.GlobalName;
                await Context.SendResponse(interactiveService, await lastFmCommand.GetNowPlaying((long)memberForUserCmd2.Id, usernameForUserCmd2, userAvatarForUserCmd2));
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
                        if (ulong.TryParse(args[1], out var parsedId))
                        {
                            var tryGetUserForUserCmd = Context.Guild?.Users.GetValueOrDefault(parsedId);
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
                        else
                        {
                            await Context.SendResponse(interactiveService, ErrorEmbed("The user you inserted is not in this server"));
                            return;
                        }
                    }
                }
                var guildMemberForUserCmd = Context.Guild?.Users.GetValueOrDefault(getUserForUserCmd.Id).AsMaybe() ?? Maybe<GuildUser>.None;
                if (guildMemberForUserCmd.HasNoValue)
                {
                    await Context.SendResponse(interactiveService, ErrorEmbed("The user you inserted is not in this server"));
                    return;
                }
                var memberForUserCmd = guildMemberForUserCmd.Value;
                var userAvatarForUserCmd = memberForUserCmd.GetGuildAvatarUrl()?.ToString(1024) ?? memberForUserCmd.GetAvatarUrl()?.ToString(1024);
                var usernameForUserCmd = memberForUserCmd.Nickname ?? memberForUserCmd.GlobalName;
                await Context.SendResponse(interactiveService, await lastFmCommand.GetUserInfo((long)memberForUserCmd.Id, usernameForUserCmd, userAvatarForUserCmd));
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
                if (ulong.TryParse(args[2], out var parsedId))
                {
                    var tryGetUserForUserCmd = Context.Guild?.Users.GetValueOrDefault(parsedId);
                    if (tryGetUserForUserCmd != null)
                    {
                        getUser = tryGetUserForUserCmd;
                    }
                    else
                    {
                        // TODO: insert env var for name or something
                        await Context.SendResponse(interactiveService, ErrorEmbed("The user you inserted is not recognised by Bento"));
                        return;
                    }
                }
                else
                {
                    await Context.SendResponse(interactiveService, ErrorEmbed("The user you inserted is not recognised by Bento"));
                    return;
                }
            }
        }

        var user = Context.Guild?.Users.GetValueOrDefault(getUser.Id).AsMaybe() ?? Maybe<GuildUser>.None;
        if (user.HasNoValue)
        {
            await Context.SendResponse(interactiveService, ErrorEmbed("The user you inserted is not in this server"));
            return;
        }

        var guildMember = user.Value;

        var userAvatar = guildMember.GetGuildAvatarUrl()?.ToString(1024) ?? guildMember.GetAvatarUrl()?.ToString(1024);

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
            case "collage":
            case "image":
            {
                string size;

                switch (args[3])
                {
                    case "1x1":
                        size = "1x1";
                        break;
                    case "2x2":
                        size = "2x2";
                        break;
                    case "3x3":
                        size = "3x3";
                        break;
                    case "4x4":
                        size = "4x4";
                        break;
                    case "5x5":
                        size = "5x5";
                        break;
                    case "6x6":
                        size = "6x6";
                        break;
                    default:
                        await Context.SendResponse(interactiveService, ErrorEmbed("Invalid size for collage. Please use `lastfm help` for a list of commands."));
                        return;
                }

                switch (args[4])
                {
                    case "topartists":
                        await Context.SendResponse(interactiveService, await lastFmCommand.GetTopArtistsCollage((long)guildMember.Id, userAvatar, period ?? "Overall", size));
                        break;
                    case "topalbums":
                        await Context.SendResponse(interactiveService, await lastFmCommand.GetTopAlbumsCollage((long)guildMember.Id, userAvatar, period ?? "Overall", size));
                        break;
                    case "toptracks":
                        await Context.SendResponse(interactiveService, await lastFmCommand.GetTopTracksCollage((long)guildMember.Id, userAvatar, period ?? "Overall", size));
                        break;
                    default:
                        await Context.SendResponse(interactiveService, ErrorEmbed("Invalid type. Please use `lastfm help` for a list of commands."));
                        break;
                }
                break;
            }
            default:
                await Context.SendResponse(interactiveService,
                    ErrorEmbed("Invalid command. Please use `lastfm help` for a list of commands."));
                break;
        }
    }

    private static ResponseModel ErrorEmbed(string error)
    {
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        embed.Embed.WithTitle(error)
            .WithColor(new Color(0xFF0000));
        return embed;
    }
}
