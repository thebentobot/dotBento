using NetCord.Rest;
using NetCord.Services.Commands;
using dotBento.Bot.Models;
using dotBento.Bot.Resources;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Commands.TextCommands;

public class BaseCommandModule(IOptions<BotEnvConfig> botSettings) : CommandModule<CommandContext>
{
    internal readonly EmbedAuthorProperties EmbedAuthor = new();
    internal readonly EmbedProperties Embed = new EmbedProperties()
        .WithColor(DiscordConstants.BentoYellow);
    internal readonly EmbedFooterProperties EmbedFooter = new();

    internal readonly BotEnvConfig BotSettings = botSettings.Value;
}
