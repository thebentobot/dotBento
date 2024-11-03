using Discord.Commands;
using dotBento.Bot.Attributes;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using Fergun.Interactive;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Commands.TextCommands;

[Name("Urban")]
public sealed class UrbanTextCommand(
    IOptions<BotEnvConfig> botSettings,
    InteractiveService interactiveService,
    UrbanCommand urbanCommand) : BaseCommandModule(botSettings)
{
    [Command("urban", RunMode = RunMode.Async)]
    [Summary("Search for a word on Urban Dictionary")]
    [Examples("urban poggers")]
    public async Task UrbanCommand([Remainder] string query)
    {
        _ = Context.Channel.TriggerTypingAsync();
        await Context.SendResponse(interactiveService, await urbanCommand.Command(query));
    }
}