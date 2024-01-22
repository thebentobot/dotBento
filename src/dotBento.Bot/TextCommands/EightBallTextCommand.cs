using Discord;
using Discord.Commands;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using dotBento.Bot.Models.Discord;
using dotBento.Infrastructure.Commands;
using Fergun.Interactive;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.TextCommands;

[Name("8ball")]
public class EightBallTextCommand(
    IOptions<BotEnvConfig> botSettings,
    InteractiveService interactiveService,
    GameCommands gameCommands) : BaseCommandModule(botSettings)
{
    [Command("8ball", RunMode = RunMode.Async)]
    [Summary("Ask the magic 8 ball a question")]
    public async Task EightBallCommand([Remainder] [Summary("Ask the magic 8 ball a question")] string question)
    {
        _ = Context.Channel.TriggerTypingAsync();
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        var embedAuthor = new EmbedAuthorBuilder()
            .WithName("Magic 8 Ball")
            .WithIconUrl("https://upload.wikimedia.org/wikipedia/commons/thumb/e/e3/8_ball_icon.svg/1200px-8_ball_icon.svg.png");
        embed.Embed.WithTitle($"\"{question}\"")
            .WithDescription($"{gameCommands.MagicEightBallResponse()}")
            .WithColor(0, 0, 0)
            .WithAuthor(embedAuthor);
        await Context.SendResponse(interactiveService, embed);
    }
}