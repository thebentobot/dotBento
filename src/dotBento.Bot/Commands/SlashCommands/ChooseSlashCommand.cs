using Discord;
using Discord.Interactions;
using dotBento.Bot.Extensions;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

[Group("choose", "Get help choosing something")]
public class ChooseSlashCommand(InteractiveService interactiveService) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("list", "Get Bento to choose from a list of options")]
    public async Task ChooseCommand(
        [Summary("options", "Write a list, separated by commas")] string options,
        [Summary("hide", "Only show the result for you")] bool? hide = false
    )
    {
        var embed = await SharedCommands.ChooseCommand.Command(options);
        var ephemeral = embed.Embed.Color == Color.Red || (hide ?? false);
        await Context.SendResponse(interactiveService, embed, ephemeral);
    }
}