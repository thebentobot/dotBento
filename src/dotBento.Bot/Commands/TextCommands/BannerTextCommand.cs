using CSharpFunctionalExtensions;
using Discord.Commands;
using Discord.WebSocket;
using dotBento.Bot.Attributes;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using Fergun.Interactive;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Commands.TextCommands;

[Name("Banner")]
public class BannerTextCommand(
    IOptions<BotEnvConfig> botSettings,
    InteractiveService interactiveService,
    BannerCommand bannerCommand) : BaseCommandModule(botSettings)
{
    [Command("banner", RunMode = RunMode.Async)]
    [Summary("Get the banner of a User Profile")]
    [Examples("banner", "banner @Banner", "banner 232584569289703424")]
    public async Task BannerCommand(SocketUser? user = null)
    {
        _ = Context.Channel.TriggerTypingAsync();
        user ??= Context.User;
        await user.ReturnIfBot(Context, interactiveService);
        var restUser = (await Context.Client.Rest.GetUserAsync(user.Id)).AsMaybe();
        await Context.SendResponse(interactiveService, await bannerCommand.Command(restUser)); 
    }   
}