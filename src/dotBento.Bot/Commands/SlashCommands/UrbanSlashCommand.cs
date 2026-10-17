using dotBento.Bot.Services;
using Discord;
using Discord.Interactions;
using dotBento.Bot.Commands.CommandHandlers;
using dotBento.Bot.Extensions;
using dotBento.Infrastructure.Services;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

public sealed class UrbanSlashCommand(InteractiveService interactiveService, UrbanCommand urbanCommand, UserSettingService userSettingService, BotPresentation? presentation = null)
    : ConfiguredInteractionModule<SocketInteractionContext>(presentation)
{
    [SlashCommand("urban", "Search Urban Dictionary for a term")]
    public async Task UrbanCommand([Summary("define", "What do you want defined?")] string query,
        [Summary("hide", "Only show user info for you")] bool? hide = null)
    {
        var embed = await urbanCommand.Command(query);
        var ephemeral = embed.CommandResponse != dotBento.Domain.Enums.CommandResponse.Ok || (hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
        await Context.SendResponse(interactiveService, embed, ephemeral);
    }
}
