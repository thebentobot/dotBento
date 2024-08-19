using CSharpFunctionalExtensions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using dotBento.Bot.Attributes;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using dotBento.Bot.Models.Discord;
using Fergun.Interactive;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Commands.TextCommands;

[Name("Member")]
public class MemberTextCommand(
    IOptions<BotEnvConfig> botSettings,
    InteractiveService interactiveService, ServerCommand serverCommand) : BaseCommandModule(botSettings)
{

    [Command("member", RunMode = RunMode.Async)]
    [Summary("Show info for a server user")]
    [Alias("guildMember")]
    [Examples("member", "member @fijispringwater", "member 229341113503318018")]
    [GuildOnly]
    public async Task MemberCommand(SocketUser? user = null)
    {
        _ = Context.Channel.TriggerTypingAsync();
        user ??= Context.User;
        await user.ReturnIfBot(Context, interactiveService);
        var guildMember = Context.Guild.Users.Single(guildUser => guildUser.Id == user.Id).AsMaybe();
        if (guildMember.HasNoValue)
        {
            var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
            embed.Embed.WithTitle($"The user you inserted is not in this server")
                .WithColor(Color.Red);
            await Context.SendResponse(interactiveService, embed);
            return;
        }
        await Context.SendResponse(interactiveService, await serverCommand.UserServerCommand(guildMember.Value));
    }
}