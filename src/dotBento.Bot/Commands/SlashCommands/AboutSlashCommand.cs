using NetCord.Services.ApplicationCommands;
using dotBento.Bot.Extensions;
using dotBento.Infrastructure.Services;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

[SlashCommand("about", "Information about stuff related to Bento")]
public sealed class AboutSlashCommand(InteractiveService interactiveService, UserSettingService userSettingService)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    [SubSlashCommand("bento", "Show info about Bento")]
    public async Task AboutCommand(
        [SlashCommandParameter(Name = "hide", Description = "Only show info for you")] bool? hide = null) =>
        await Context.SendResponse(interactiveService, await SharedCommands.AboutCommand.Command(Context), hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
}
