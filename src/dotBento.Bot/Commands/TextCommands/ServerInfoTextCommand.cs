using Discord.Commands;
using dotBento.Bot.Attributes;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using Fergun.Interactive;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Commands.TextCommands
{
    [Name("ServerInfo")]
    public class ServerInfoTextCommand(
        IOptions<BotEnvConfig> botSettings,
        InteractiveService interactiveService,
        ServerCommand serverCommand) : BaseCommandModule(botSettings)
    {

        [Command("serverInfo", RunMode = RunMode.Async)]
        [Summary("Show info for a server")]
        [Alias("guildInfo")]
        [Examples("serverInfo", "guildInfo")]
        [GuildOnly]
        public async Task ServerInfoCommand()
        {
            _ = Context.Channel.TriggerTypingAsync();
            await Context.SendResponse(interactiveService, await serverCommand.ServerInfoCommand(Context.Guild));
        }
    }
}