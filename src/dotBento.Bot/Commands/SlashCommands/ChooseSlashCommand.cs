using dotBento.Bot.Services;
using Discord;
using Discord.Interactions;
using dotBento.Bot.Extensions;
using dotBento.Infrastructure.Services;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

[Group("choose", "Get help choosing something")]
public sealed class ChooseSlashCommand(InteractiveService interactiveService, UserSettingService userSettingService, BotPresentation? presentation = null) : ConfiguredInteractionModule<SocketInteractionContext>(presentation)
{
    [SlashCommand("list", "Get Bento to choose from a list of options")]
    public async Task ChooseCommand(
        [Summary("options", "Write a list, separated by commas")] string options,
        [Summary("hide", "Only show the result for you")] bool? hide = null
    )
    {
        var embed = await CommandHandlers.ChooseCommand.Command(options, Presentation);
        var ephemeral = embed.CommandResponse != dotBento.Domain.Enums.CommandResponse.Ok || (hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
        await Context.SendResponse(interactiveService, embed, ephemeral);
    }
}
