using Discord.Commands;
using dotBento.Bot.Attributes;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using Fergun.Interactive;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Commands.TextCommands;

[Name("Choose")]
public sealed class ChooseTextCommand(
    IOptions<BotEnvConfig> botSettings,
    InteractiveService interactiveService) : BaseCommandModule(botSettings)
{
    [Command("choose", RunMode = RunMode.Async)]
    [Summary("List the options and get a choice")]
    [Alias("pick")]
    [Examples("choose option1, option2, option3")]
    public async Task ChooseCommand([Remainder] [Summary("List of options to choose between")] string options)
    {
        _ = Context.Channel.TriggerTypingAsync();
        await Context.SendResponse(interactiveService, await SharedCommands.ChooseCommand.Command(options));
    }
}