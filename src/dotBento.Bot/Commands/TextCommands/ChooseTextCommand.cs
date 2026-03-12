using NetCord.Services.Commands;
using dotBento.Bot.Attributes;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using Fergun.Interactive;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Commands.TextCommands;

[ModuleName("Choose")]
public sealed class ChooseTextCommand(
    IOptions<BotEnvConfig> botSettings,
    InteractiveService interactiveService) : BaseCommandModule(botSettings)
{
    [Command("choose", "pick")]
    [Summary("List the options and get a choice")]
    [Examples("choose option1, option2, option3")]
    public async Task ChooseCommand([CommandParameter(Remainder = true)] [Summary("List of options to choose between")] string options)
    {
        _ = Context.Channel?.TriggerTypingStateAsync();
        await Context.SendResponse(interactiveService, await SharedCommands.ChooseCommand.Command(options));
    }
}
