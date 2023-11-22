using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Serilog;

namespace dotBento.Bot.Handlers;

public class InteractionHandler(DiscordSocketClient client,
    InteractionService interactionService,
    IServiceProvider services)
{
    public async Task InitializeAsync()
    {
        await interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), services);

        client.InteractionCreated += HandleInteraction;
        interactionService.InteractionExecuted += HandleInteractionExecuted;
    }

    private async Task HandleInteraction(SocketInteraction interaction)
    {
        try
        {
            var context = new SocketInteractionContext(client, interaction);

            var result = await interactionService.ExecuteCommandAsync(context, services);

            if (!result.IsSuccess)
                _ = Task.Run(() => HandleInteractionExecutionResult(interaction, result));
        }
        catch (Exception ex)
        {
            Log.Error(ex, ex?.Message);
        }
    }

    private Task HandleInteractionExecuted(ICommandInfo command, IInteractionContext context, IResult result)
    {
        if (!result.IsSuccess)
            _ = Task.Run(() => HandleInteractionExecutionResult(context.Interaction, result));
        return Task.CompletedTask;
    }

    private async Task HandleInteractionExecutionResult(IDiscordInteraction interaction, IResult result)
    {
        switch (result.Error)
        {
            case InteractionCommandError.UnmetPrecondition:
                Log.Error($"Unmet precondition - {result.Error}");
                break;

            case InteractionCommandError.BadArgs:
                Log.Error($"Unmet precondition - {result.Error}");
                break;

            case InteractionCommandError.ConvertFailed:
                Log.Error($"Convert Failed - {result.Error}");
                break;

            case InteractionCommandError.Exception:
                Log.Error($"Exception - {result.Error}");
                break;

            case InteractionCommandError.ParseFailed:
                Log.Error($"Parse Failed - {result.Error}");
                break;

            case InteractionCommandError.UnknownCommand:
                Log.Error($"Unknown Command - {result.Error}");
                break;

            case InteractionCommandError.Unsuccessful:
                Log.Error($"Unsuccessful - {result.Error}");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        const string errorMsg = "An error has occurred. We are already investigating it!";

        if (!interaction.HasResponded)
        {
            await interaction.RespondAsync(errorMsg, ephemeral: true);
        }
        else
        {
            await interaction.FollowupAsync(errorMsg, ephemeral: true);
        }
    }
}