using NetCord;
using NetCord.Services.Commands;
using dotBento.Bot.Attributes;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using Fergun.Interactive;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Commands.TextCommands;

[ModuleName("User")]
public sealed class UserTextCommand(
    IOptions<BotEnvConfig> botSettings,
    InteractiveService interactiveService, UserCommand userCommand) : BaseCommandModule(botSettings)
{

    [Command("user")]
    [Summary("Show info for a user")]
    [Examples("user", "user @Lewis", "user 166142440233893888")]
    public async Task UserCommand(User? user = null)
    {
        _ = Context.Channel?.TriggerTypingAsync();
        user ??= Context.User;
        await user.ReturnIfBot(Context, interactiveService);
        await Context.SendResponse(interactiveService, await userCommand.Command(user));
    }

    [Command("profile", "rank")]
    [Summary("Show profile for a user. Customisable profile coming back very soon.")]
    [Examples("profile", "user @Adam", "user 223908083825377281")]
    [GuildOnly]
    public async Task ProfileCommand(User? user = null)
    {
        _ = Context.Channel?.TriggerTypingAsync();
        user ??= Context.User;
        await user.ReturnIfBot(Context, interactiveService);
        var guildMember = Context.Guild?.Users.GetValueOrDefault(user.Id);
        var botPfp = Context.Client.Cache.User?.GetAvatarUrl()?.ToString(1024) ?? string.Empty;
        await Context.SendResponse(interactiveService,
            await userCommand.GetProfileAsync((long)user.Id, (long)Context.Guild!.Id, guildMember,
                Context.Guild.UserCount, botPfp));
    }
}
