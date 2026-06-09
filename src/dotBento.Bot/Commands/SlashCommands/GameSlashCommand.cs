using Discord;
using Discord.Interactions;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using dotBento.Domain.Enums.Games;
using dotBento.Infrastructure.Services;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

[Group("game", "Play a game")]
public sealed class GameSlashCommand(
    InteractiveService interactiveService,
    GameCommand gameCommand,
    UserSettingService userSettingService) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("rps", "Play Rock Paper Scissors")]
    public async Task RpsCommand(
        [Summary("weapon", "Do you pick rock, paper or scissors?")] RpsGameChoice choice,
        [Summary("hide", "Only show the result for you")] bool? hide = null
    ) =>
        await Context.SendResponse(interactiveService, await gameCommand.RpsCommand(choice, (long)Context.User.Id), hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));

    [SlashCommand("8ball", "Ask the magic 8 ball a question")]
    public async Task EightBallCommand(
        [Summary("question", "Ask the magic 8 ball a question")] string question,
        [Summary("hide", "Only show the result for you")] bool? hide = null
    ) =>
        await Context.SendResponse(interactiveService, await GameCommand.MagicEightBallCommand(question), hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));

    [SlashCommand("roll", "Roll a random number")]
    public async Task RollCommand(
        [Summary("min", "The minimum number to roll (default = 1)")] int? min = 1,
        [Summary("max", "The maximum number to roll (default = 10)")] int? max = 10,
        [Summary("hide", "Only show the result for you")] bool? hide = null
    )
    {
        var embed = await GameCommand.RollCommand(min, max);
        var ephemeral = embed.Embed.Color == Color.Red || (hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
        await Context.SendResponse(interactiveService, embed, ephemeral);
    }
}
