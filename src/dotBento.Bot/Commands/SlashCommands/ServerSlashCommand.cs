using CSharpFunctionalExtensions;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Attributes;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models.Discord;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

[Group("server", "Commands for the Discord Server")]
public sealed class ServerSlashCommand(InteractiveService interactiveService, ServerCommand serverCommand)
    : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("user", "Show info for a server user")]
    [GuildOnly]
    public async Task UserServerCommand([Summary("user", "Pick a User")] SocketUser? user = null,
        [Summary("hide", "Only show server user info for you")] bool? hide = false)
    {
        user ??= Context.User;
        await user.ReturnIfBot(Context, interactiveService);
        var guildMember = Context.Guild.Users.Single(guildUser => guildUser.Id == user.Id).AsMaybe();
        if (guildMember.HasNoValue)
        {
            var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
            embed.Embed.WithTitle($"The user you inserted is not in this server")
                .WithColor(Color.Red);
            await Context.SendResponse(interactiveService, embed, true);
            return;
        }
        await Context.SendResponse(interactiveService, await serverCommand.UserServerCommand(guildMember.Value), hide ?? false);
    }
    
    [SlashCommand("info", "Show info for the server")]
    [GuildOnly]
    public async Task ServerCommand([Summary("hide", "Only show server info for you")] bool? hide = false) =>
        await Context.SendResponse(interactiveService, await serverCommand.ServerInfoCommand(Context.Guild),
            hide ?? false);
}