using Discord.Interactions;
using dotBento.Bot.Extensions;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

[Group("about", "Information about stuff related to Bento")]
public sealed class AboutSlashCommand(InteractiveService interactiveService)
    : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("bento", "Show info about Bento")]
    public async Task AboutCommand(
        [Summary("hide", "Only show info for you")] bool? hide = false) =>
        await Context.SendResponse(interactiveService, await SharedCommands.AboutCommand.Command(Context), hide ?? false);
}