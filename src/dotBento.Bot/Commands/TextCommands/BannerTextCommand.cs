using NetCord;
using NetCord.Services.Commands;
using dotBento.Bot.Attributes;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using dotBento.Bot.Services;
using Fergun.Interactive;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Commands.TextCommands;

[ModuleName("Banner")]
public sealed class BannerTextCommand(
    IOptions<BotEnvConfig> botSettings,
    InteractiveService interactiveService,
    BannerCommand bannerCommand,
    IDiscordUserResolver userResolver) : BaseCommandModule(botSettings)
{
    [Command("banner")]
    [Summary("Get the banner of a User Profile")]
    [Examples("banner", "banner @Banner", "banner 232584569289703424")]
    public async Task BannerCommand(User? user = null)
    {
        _ = Context.Channel?.TriggerTypingAsync();
        user ??= Context.User;
        await user.ReturnIfBot(Context, interactiveService);
        var restUser = await userResolver.GetRestUserAsync(user.Id);
        await Context.SendResponse(interactiveService, await bannerCommand.Command(restUser));
    }
}
