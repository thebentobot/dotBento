using CSharpFunctionalExtensions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using dotBento.Bot.Models.Discord;
using Fergun.Interactive;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.TextCommands;

[Name("Banner")]
public class BannerTextCommand(
    IOptions<BotEnvConfig> botSettings,
    InteractiveService interactiveService) : BaseCommandModule(botSettings)
{
    [Command("banner", RunMode = RunMode.Async)]
    [Summary("Get the banner of a User Profile")]
    public async Task BannerCommand([Summary("The user to get the User Profile Banner from")] SocketUser? user = null)
    {
        _ = Context.Channel.TriggerTypingAsync();
        user ??= Context.User;
        var restUser = (await Context.Client.Rest.GetUserAsync(user.Id)).AsMaybe();
        if (restUser.HasNoValue)
        {
            await Context.SendResponse(interactiveService, new ResponseModel{ ResponseType = ResponseType.Text, Text = "Sorry, I couldn't find that user." });
        } else
        {
            if (restUser.Value.GetBannerUrl(ImageFormat.Auto, 2048) == null)
            {
                await Context.SendResponse(interactiveService, new ResponseModel{ ResponseType = ResponseType.Text, Text = "Sorry, that user doesn't have a banner." });
                return;
            }
            var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
            embed.Embed.WithTitle($"{user.Username}'s User Profile Banner")
                .WithImageUrl(restUser.Value.GetBannerUrl(size: 2048, format: ImageFormat.Auto));
            await Context.SendResponse(interactiveService, embed); 
        }   
    }   
}