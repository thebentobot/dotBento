using CSharpFunctionalExtensions;
using NetCord;
using NetCord.Services.Commands;
using dotBento.Bot.Attributes;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Services;
using Fergun.Interactive;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Commands.TextCommands;

[ModuleName("Member")]
public sealed class MemberTextCommand(
    IOptions<BotEnvConfig> botSettings,
    InteractiveService interactiveService, ServerCommand serverCommand) : BaseCommandModule(botSettings)
{

    [Command("member", "guildMember")]
    [Summary("Show info for a server user")]
    [Examples("member", "member @fijispringwater", "member 229341113503318018")]
    [GuildOnly]
    public async Task MemberCommand(User? user = null)
    {
        _ = Context.Channel?.TriggerTypingAsync();
        user ??= Context.User;
        await user.ReturnIfBot(Context, interactiveService);
        var guildMember = (Context.Guild?.Users.GetValueOrDefault(user.Id)).AsMaybe();
        if (guildMember.HasNoValue)
        {
            await Context.SendResponse(interactiveService, GenericEmbedService.ErrorEmbed("The user you inserted is not in this server"));
            return;
        }
        await Context.SendResponse(interactiveService, await serverCommand.UserServerCommand(guildMember.Value, Context.Guild!));
    }
}
