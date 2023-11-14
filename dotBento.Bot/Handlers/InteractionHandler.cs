using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Serilog;

namespace dotBento.Bot.Handlers;

public class InteractionHandler
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactionService;
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    public InteractionHandler(DiscordSocketClient client,
        InteractionService interactionService,
        IServiceProvider services,
        ILogger logger)
    {
        _client = client;
        _interactionService = interactionService;
        _services = services;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

        _client.InteractionCreated += HandleInteraction;
        _interactionService.InteractionExecuted += HandleInteractionExecuted;
    }

    private async Task HandleInteraction(SocketInteraction interaction)
    {
        try
        {
            var context = new SocketInteractionContext(_client, interaction);

            var result = await _interactionService.ExecuteCommandAsync(context, _services);

            if (!result.IsSuccess)
                _ = Task.Run(() => HandleInteractionExecutionResult(interaction, result));
        }
        catch (Exception ex)
        {
            _logger.Error(ex, ex?.Message);
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
                _logger.Error($"Unmet precondition - {result.Error}");
                break;

            case InteractionCommandError.BadArgs:
                _logger.Error($"Unmet precondition - {result.Error}");
                break;

            case InteractionCommandError.ConvertFailed:
                _logger.Error($"Convert Failed - {result.Error}");
                break;

            case InteractionCommandError.Exception:
                _logger.Error($"Exception - {result.Error}");
                break;

            case InteractionCommandError.ParseFailed:
                _logger.Error($"Parse Failed - {result.Error}");
                break;

            case InteractionCommandError.UnknownCommand:
                _logger.Error($"Unknown Command - {result.Error}");
                break;

            case InteractionCommandError.Unsuccessful:
                _logger.Error($"Unsuccessful - {result.Error}");
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