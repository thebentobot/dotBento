using Discord;
using Discord.Commands;
using Discord.WebSocket;
using dotBento.Bot.Attributes;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using dotBento.Bot.Models.Discord;
using Fergun.Interactive;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.TextCommands;

[Name("Avatar")]
public class AvatarTextCommand(
    IOptions<BotEnvConfig> botSettings,
    InteractiveService interactiveService) : BaseCommandModule(botSettings)
{
    [Command("avatar", RunMode = RunMode.Async)]
    [Summary("Get the avatar of a user")]
    [Alias("av", "pfp")]
    [GuildOnly]
    public async Task AvatarCommand([Summary("The user to get avatar from")] SocketUser user = null)
    {
        _ = Context.Channel.TriggerTypingAsync();
        user ??= Context.User;
        var guildMember = Context.Guild.Users.Single(guildUser => guildUser.Id == user.Id);
        
        var userProfileEmbed = new ResponseModel{ ResponseType = ResponseType.Embed };
        userProfileEmbed.Embed.WithTitle($"{user.Username}'s User Profile avatar")
            .WithImageUrl(user.GetAvatarUrl(size: 2048, format: ImageFormat.Auto));

        if (user.GetDefaultAvatarUrl() == guildMember.GetDisplayAvatarUrl())
        {
            await Context.SendResponse(interactiveService, userProfileEmbed);
        }
        else
        {
            
            var serverProfileEmbed = new ResponseModel{ ResponseType = ResponseType.Embed };
            serverProfileEmbed.Embed.WithTitle($"{guildMember.Nickname}'s Server Profile avatar")
                .WithImageUrl(guildMember.GetGuildAvatarUrl(size: 2048, format: ImageFormat.Auto));
            
            await Context.SendResponse(interactiveService, serverProfileEmbed);
            await Context.SendResponse(interactiveService, userProfileEmbed);
        }
    }
}