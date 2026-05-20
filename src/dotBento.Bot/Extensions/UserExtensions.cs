using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Enums;
using dotBento.Bot.Models.Discord;
using Fergun.Interactive;

namespace dotBento.Bot.Extensions;

public static class UserExtensions
{
    private static ResponseModel CreateBotResponseModel()
    {
        return new ResponseModel
        {
            ResponseType = ResponseType.Text,
            Text = "Bots can't be used as a user argument."
        };
    }

    public static Task ReturnIfBot(this SocketUser user, SocketInteractionContext context, InteractiveService interactiveService)
    {
        if (user.IsBot)
        {
            return context.SendResponse(interactiveService, CreateBotResponseModel(), true);
        }
        return Task.CompletedTask;
    }

    public static Task ReturnIfBot(this SocketUser user, SocketCommandContext context, InteractiveService interactiveService)
    {
        if (user.IsBot)
        {
            return context.SendResponse(interactiveService, CreateBotResponseModel());
        }
        return Task.CompletedTask;
    }
    
    // TODO create a command that creates the user if they don't exist. Though this only needs to be used for commands where they using themselves as the user argument.
}