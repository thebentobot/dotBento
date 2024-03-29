using Discord;
using Discord.Commands;
using dotBento.Bot.Models;
using dotBento.Bot.Resources;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Commands.TextCommands;

public class BaseCommandModule(IOptions<BotEnvConfig> botSettings) : ModuleBase<SocketCommandContext>
{
    internal readonly EmbedAuthorBuilder EmbedAuthor = new();
    internal readonly EmbedBuilder Embed = new EmbedBuilder()
        .WithColor(DiscordConstants.BentoYellow);
    internal readonly EmbedFooterBuilder EmbedFooter = new();

    internal readonly BotEnvConfig BotSettings = botSettings.Value;
}