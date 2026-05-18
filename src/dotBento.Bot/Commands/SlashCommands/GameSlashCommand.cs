using NetCord;
using NetCord.Services.ApplicationCommands;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using dotBento.Domain.Enums.Games;
using dotBento.Infrastructure.Services;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

[SlashCommand("game", "Play a game")]
public sealed class GameSlashCommand(
    InteractiveService interactiveService,
    GameCommand gameCommand,
    UserSettingService userSettingService) : ApplicationCommandModule<ApplicationCommandContext>
{
    [SubSlashCommand("rps", "Play Rock Paper Scissors")]
    public async Task RpsCommand(
        [SlashCommandParameter(Name = "weapon", Description = "Do you pick rock, paper or scissors?")] RpsGameChoice choice,
        [SlashCommandParameter(Name = "hide", Description = "Only show the result for you")] bool? hide = null
    ) =>
        await Context.SendResponse(interactiveService, await gameCommand.RpsCommand(choice, (long)Context.User.Id), hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));

    [SubSlashCommand("8ball", "Ask the magic 8 ball a question")]
    public async Task EightBallCommand(
        [SlashCommandParameter(Name = "question", Description = "Ask the magic 8 ball a question")] string question,
        [SlashCommandParameter(Name = "hide", Description = "Only show the result for you")] bool? hide = null
    ) =>
        await Context.SendResponse(interactiveService, await GameCommand.MagicEightBallCommand(question), hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));

    [SubSlashCommand("roll", "Roll a random number")]
    public async Task RollCommand(
        [SlashCommandParameter(Name = "min", Description = "The minimum number to roll (default = 1)")] int? min = 1,
        [SlashCommandParameter(Name = "max", Description = "The maximum number to roll (default = 10)")] int? max = 10,
        [SlashCommandParameter(Name = "hide", Description = "Only show the result for you")] bool? hide = null
    )
    {
        var embed = await GameCommand.RollCommand(min, max);
        var ephemeral = embed.Embed.Color == new Color(0xFF0000) || (hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
        await Context.SendResponse(interactiveService, embed, ephemeral);
    }
}
