using NetCord;
using NetCord.Services.ApplicationCommands;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using dotBento.Infrastructure.Services;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

public sealed class UrbanSlashCommand(InteractiveService interactiveService, UrbanCommand urbanCommand, UserSettingService userSettingService)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("urban", "Search Urban Dictionary for a term")]
    public async Task UrbanCommand([SlashCommandParameter(Name = "define", Description = "What do you want defined?")] string query,
        [SlashCommandParameter(Name = "hide", Description = "Only show user info for you")] bool? hide = null)
    {
        var embed = await urbanCommand.Command(query);
        var ephemeral = embed.Embed.Color == new Color(0xFF0000) || (hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
        await Context.SendResponse(interactiveService, embed, ephemeral);
    }
}
