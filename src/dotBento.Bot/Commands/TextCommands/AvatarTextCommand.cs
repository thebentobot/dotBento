using Discord.Commands;
using Discord.WebSocket;
using dotBento.Bot.Attributes;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using Fergun.Interactive;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Commands.TextCommands;

[Name("Avatar")]
public sealed class AvatarTextCommand(
    IOptions<BotEnvConfig> botSettings,
    InteractiveService interactiveService,
    AvatarCommand avatarCommand) : BaseCommandModule(botSettings)
{
    [Command("avatar", RunMode = RunMode.Async)]
    [Summary("Get the avatar of a User Profile")]
    [Alias("av", "pfp")]
    [Examples("avatar", "av 223908083825377281", "pfp @Adam")]
    [GuildOnly]
    public async Task AvatarCommand(SocketUser? user = null)
    {
        _ = Context.Channel.TriggerTypingAsync();
        user ??= Context.User;
        await user.ReturnIfBot(Context, interactiveService);
        var guildMember = Context.Guild.Users.Single(guildUser => guildUser.Id == user.Id);

        if (user.GetDisplayAvatarUrl() == guildMember.GetDisplayAvatarUrl())
        {
            await Context.SendResponse(interactiveService, await avatarCommand.UserAvatarCommand(user));
        }
        else
        {
            
            await Context.SendResponse(interactiveService, await avatarCommand.ServerAvatarCommand(guildMember));
            await Context.SendResponse(interactiveService, await avatarCommand.UserAvatarCommand(user));
        }
    }
}