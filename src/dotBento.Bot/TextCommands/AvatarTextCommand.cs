using Discord;
using Discord.Commands;
using Discord.WebSocket;
using dotBento.Bot.Attributes;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using dotBento.Bot.Models.Discord;
using dotBento.Infrastructure.Utilities;
using Fergun.Interactive;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.TextCommands;

[Name("Avatar")]
public class AvatarTextCommand(
    IOptions<BotEnvConfig> botSettings,
    InteractiveService interactiveService,
    StylingUtilities stylingUtilities) : BaseCommandModule(botSettings)
{
    [Command("avatar", RunMode = RunMode.Async)]
    [Summary("Get the avatar of a User Profile")]
    [Alias("av", "pfp")]
    [GuildOnly]
    public async Task AvatarCommand([Summary("The user to get the User Profile Avatar from")] SocketUser user = null)
    {
        _ = Context.Channel.TriggerTypingAsync();
        user ??= Context.User;
        await user.ReturnIfBot(Context, interactiveService);
        var guildMember = Context.Guild.Users.Single(guildUser => guildUser.Id == user.Id);
        
        var userProfileEmbed = new ResponseModel{ ResponseType = ResponseType.Embed };
        var userPfpColour = await stylingUtilities.GetDominantColorAsync(user.GetAvatarUrl(ImageFormat.WebP, 128));
        userProfileEmbed.Embed.WithTitle($"{user.GlobalName}'s User Profile avatar")
            .WithColor(userPfpColour)
            .WithImageUrl(user.GetAvatarUrl(size: 2048, format: ImageFormat.Auto));

        if (user.GetDefaultAvatarUrl() == guildMember.GetDisplayAvatarUrl())
        {
            await Context.SendResponse(interactiveService, userProfileEmbed);
        }
        else
        {
            
            var serverProfileEmbed = new ResponseModel{ ResponseType = ResponseType.Embed };
            var guildMemberPfpColour = await stylingUtilities.GetDominantColorAsync(guildMember.GetGuildAvatarUrl(ImageFormat.WebP, 128));
            serverProfileEmbed.Embed.WithTitle($"{guildMember.Nickname ?? guildMember.DisplayName}'s Server Profile avatar")
                .WithImageUrl(guildMember.GetGuildAvatarUrl(size: 2048, format: ImageFormat.Auto));
            
            await Context.SendResponse(interactiveService, serverProfileEmbed);
            await Context.SendResponse(interactiveService, userProfileEmbed);
        }
    }
}