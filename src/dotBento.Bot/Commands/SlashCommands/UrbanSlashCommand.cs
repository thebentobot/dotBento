using Discord;
using Discord.Interactions;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

public sealed class UrbanSlashCommand(InteractiveService interactiveService, UrbanCommand urbanCommand)
    : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("urban", "Search Urban Dictionary for a term")]
    public async Task UrbanCommand([Summary("define", "What do you want defined?")] string query,
        [Summary("hide", "Only show user info for you")] bool? hide = false)
    {
        var embed = await urbanCommand.Command(query);
        var ephemeral = embed.Embed.Color == Color.Red || (hide ?? false);
        await Context.SendResponse(interactiveService, embed, ephemeral);
    }
}