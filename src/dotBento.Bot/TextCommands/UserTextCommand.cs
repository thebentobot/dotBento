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

namespace dotBento.Bot.TextCommands
{
    [Name("User")]
    public class UserTextCommand(
        IOptions<BotEnvConfig> botSettings,
        InteractiveService interactiveService, StylingUtilities stylingUtilities) : BaseCommandModule(botSettings)
    {

        [Command("user", RunMode = RunMode.Async)]
        [Summary("Show info for a user")]
        public async Task UserCommand([Summary("The user to get the User info from")] SocketUser user = null)
        {
            _ = Context.Channel.TriggerTypingAsync();
            user ??= Context.User;
            await user.ReturnIfBot(Context, interactiveService);
            var response = new ResponseModel { ResponseType = ResponseType.Embed };
            var userPfpColour = await stylingUtilities.GetDominantColorAsync(user.GetAvatarUrl(ImageFormat.WebP, 128));
            response.Embed
                .WithTitle($"Profile for {user.Username}")
                .WithColor(userPfpColour)
                .WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
                .AddField("Username (global name)", user.GlobalName)
                .AddField("User ID", user.Id.ToString())
                .AddField("Account created on", $"<t:{user.CreatedAt.ToUnixTimeSeconds()}:F>");

            await Context.SendResponse(interactiveService, response);
        }
    }
}