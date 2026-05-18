using Fergun.Interactive;
using NetCord;
using NetCord.Services.ApplicationCommands;
using NetCord.Services.Commands;
using dotBento.Bot.Enums;
using dotBento.Bot.Models.Discord;

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

    public static Task ReturnIfBot(this User? user, ApplicationCommandContext context, InteractiveService interactiveService)
    {
        if (user is null || user.IsBot)
        {
            return context.SendResponse(interactiveService, CreateBotResponseModel(), true);
        }
        return Task.CompletedTask;
    }

    public static Task ReturnIfBot(this User? user, CommandContext context, InteractiveService interactiveService)
    {
        if (user is null || user.IsBot)
        {
            return context.SendResponse(interactiveService, CreateBotResponseModel());
        }
        return Task.CompletedTask;
    }

    // TODO create a command that creates the user if they don't exist. Though this only needs to be used for commands where they using themselves as the user argument.
}
