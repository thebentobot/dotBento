using NetCord;
using NetCord.Services.ApplicationCommands;
using dotBento.Bot.Extensions;
using dotBento.Infrastructure.Services;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

[SlashCommand("choose", "Get help choosing something")]
public sealed class ChooseSlashCommand(InteractiveService interactiveService, UserSettingService userSettingService) : ApplicationCommandModule<ApplicationCommandContext>
{
    [SubSlashCommand("list", "Get Bento to choose from a list of options")]
    public async Task ChooseCommand(
        [SlashCommandParameter(Name = "options", Description = "Write a list, separated by commas")] string options,
        [SlashCommandParameter(Name = "hide", Description = "Only show the result for you")] bool? hide = null
    )
    {
        var embed = await SharedCommands.ChooseCommand.Command(options);
        var ephemeral = embed.Embed.Color == new Color(0xFF0000) || (hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
        await Context.SendResponse(interactiveService, embed, ephemeral);
    }
}
