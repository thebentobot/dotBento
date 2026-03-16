using NetCord;
using NetCord.Services.Commands;
using dotBento.Bot.Attributes;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using Fergun.Interactive;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Commands.TextCommands;

[ModuleName("Avatar")]
public sealed class AvatarTextCommand(
    IOptions<BotEnvConfig> botSettings,
    InteractiveService interactiveService,
    AvatarCommand avatarCommand) : BaseCommandModule(botSettings)
{
    [Command("avatar", "av", "pfp")]
    [Summary("Get the avatar of a User Profile")]
    [Examples("avatar", "av 223908083825377281", "pfp @Adam")]
    [GuildOnly]
    public async Task AvatarCommand(User? user = null)
    {
        _ = Context.Channel?.TriggerTypingAsync();
        user ??= Context.User;
        await user.ReturnIfBot(Context, interactiveService);
        var guildMember = Context.Guild?.Users.GetValueOrDefault(user.Id);

        if (guildMember == null || user.GetAvatarUrl() == guildMember.GetGuildAvatarUrl())
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
