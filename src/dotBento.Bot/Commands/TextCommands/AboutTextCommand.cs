using Discord.Commands;
using dotBento.Bot.Attributes;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using Fergun.Interactive;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Commands.TextCommands
{
    [Name("About")]
    public class AboutTextCommand(
        IOptions<BotEnvConfig> botSettings,
        InteractiveService interactiveService) : BaseCommandModule(botSettings)
    {

        [Command("about", RunMode = RunMode.Async)]
        [Summary("Show info about Bento")]
        [Examples("about")]
        public async Task AboutCommand()
        {
            _ = Context.Channel.TriggerTypingAsync();
            await Context.SendResponse(interactiveService, await SharedCommands.AboutCommand.Command(Context));
        }
    }
}