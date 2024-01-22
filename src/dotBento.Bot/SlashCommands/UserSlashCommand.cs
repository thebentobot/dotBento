using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models.Discord;
using dotBento.EntityFramework.Context;
using dotBento.Infrastructure.Utilities;
using Fergun.Interactive;

namespace dotBento.Bot.SlashCommands;

[Group("user", "Commmands for Discord Users")]
public class UserSlashCommand(BotDbContext botDbContext, InteractiveService interactiveService, StylingUtilities stylingUtilities)
    : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("info", "Show info for a user")]
    public async Task UserCommand([Summary("user", "Pick a User")] SocketUser? user = null,
        [Summary("hide", "Only show user info for you")] bool? hide = false)
    {
        user ??= Context.User;
        await user.ReturnIfBot(Context, interactiveService);
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        var userPfpColour = await stylingUtilities.GetDominantColorAsync(user.GetAvatarUrl(ImageFormat.WebP, 128));
        embed.Embed
            .WithColor(userPfpColour)
            .WithTitle($"Profile for {user.GlobalName}")
            .WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
            .AddField("Username", user.Username)
            .AddField("User ID", user.Id.ToString())
            .AddField("Account created on", $"<t:{user.CreatedAt.ToUnixTimeSeconds()}:F>");
        
        await Context.SendResponse(interactiveService, embed, hide ?? false);
    }
}