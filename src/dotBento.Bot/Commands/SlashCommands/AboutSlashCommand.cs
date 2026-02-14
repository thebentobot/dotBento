using Discord.Interactions;
using dotBento.Bot.Extensions;
using dotBento.Infrastructure.Services;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

[Group("about", "Information about stuff related to Bento")]
public sealed class AboutSlashCommand(InteractiveService interactiveService, UserSettingService userSettingService)
    : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("bento", "Show info about Bento")]
    public async Task AboutCommand(
        [Summary("hide", "Only show info for you")] bool? hide = null) =>
        await Context.SendResponse(interactiveService, await SharedCommands.AboutCommand.Command(Context), hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
}