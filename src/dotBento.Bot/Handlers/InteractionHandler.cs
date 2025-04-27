using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Attributes;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models.Discord;
using dotBento.Domain;
using dotBento.Domain.Enums;
using Fergun.Interactive;
using Prometheus;
using Serilog;

namespace dotBento.Bot.Handlers;

public sealed class InteractionHandler
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactionService;
    private readonly InteractiveService _fergunInteractiveService;
    private readonly IServiceProvider _provider;

    public InteractionHandler(DiscordSocketClient client,
        InteractionService interactionService,
        IServiceProvider provider,
        InteractiveService fergunInteractiveService)
    {
        _client = client;
        _interactionService = interactionService;
        _provider = provider;
        _fergunInteractiveService = fergunInteractiveService;
        _client.SlashCommandExecuted += SlashCommandExecuted;
        _client.AutocompleteExecuted += AutoCompleteExecuted;
        _client.SelectMenuExecuted += SelectMenuExecuted;
        _client.ModalSubmitted += ModalSubmitted;
        _client.UserCommandExecuted += UserCommandExecuted;
        _client.ButtonExecuted += ButtonExecuted;
    }

    private async Task SlashCommandExecuted(SocketInteraction socketInteraction)
    {
        Statistics.DiscordEvents.WithLabels(nameof(SlashCommandExecuted)).Inc();

        if (socketInteraction is not SocketSlashCommand socketSlashCommand)
        {
            return;
        }

        using (Statistics.SlashCommandHandlerDuration.NewTimer())
        {
            var context = new SocketInteractionContext(_client, socketInteraction);

            var commandSearch = _interactionService.SearchSlashCommand(socketSlashCommand);

            if (!commandSearch.IsSuccess)
            {
                Log.Error("Someone tried to execute a non-existent slash command! {SlashCommand}",
                    socketSlashCommand.CommandName);
                return;
            }

            var command = commandSearch.Command;
            
            var keepGoing = await CheckAttributes(context, commandSearch.Command.Attributes);

            if (!keepGoing)
            {
                return;
            }

            var result = await _interactionService.ExecuteCommandAsync(context, _provider);
            
            if (result.IsSuccess)
            {
                Statistics.SlashCommandsExecuted.WithLabels(command.Name).Inc();
                // TODO _ = Task.Run(() => _userService.AddUserSlashCommandInteraction(context, command.Name));
            } else switch (result.Error)
            {
                case InteractionCommandError.ParseFailed:
                {
                    Statistics.SlashCommandsFailed.WithLabels(command.Name).Inc();
                    var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
                    embed.Embed.WithTitle("Error: Invalid input")
                        .WithDescription($"{result.ErrorReason}")
                        .WithColor(Color.Red);
                    await context.SendResponse(_fergunInteractiveService, embed);
                    break;
                }
                case InteractionCommandError.BadArgs:
                {
                    Statistics.SlashCommandsFailed.WithLabels(command.Name).Inc();
                    var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
                    embed.Embed.WithTitle("Error: Bad argument count")
                        .WithDescription($"You have provided too many or too few arguments for the command `{command.Name}`")
                        .WithColor(Color.Red);
                    await context.SendResponse(_fergunInteractiveService, embed);
                    break;
                }
                case InteractionCommandError.Exception:
                {
                    Statistics.SlashCommandsFailed.WithLabels(command.Name).Inc();
                    var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
                    embed.Embed.WithTitle("Error: Exception")
                        .WithDescription($"An exception occurred while executing the command `{command.Name}`\nDon't worry, the developers have been notified and will fix it as soon as possible")
                        .WithColor(Color.Red);
                    await context.SendResponse(_fergunInteractiveService, embed);
                    break;
                }
                case InteractionCommandError.Unsuccessful:
                {
                    Statistics.SlashCommandsFailed.WithLabels(command.Name).Inc();
                    var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
                    embed.Embed.WithTitle("Error: Unsuccessful")
                        .WithDescription($"The command `{command.Name}` was unsuccessful. Don't worry, the developers have been notified and will fix it as soon as possible")
                        .WithColor(Color.Red);
                    await context.SendResponse(_fergunInteractiveService, embed);
                    break;
                }
                // TODO would be nice to log when one of these errors below gets hit
                // ReSharper disable once RedundantCaseLabel
                case null:
                // ReSharper disable once RedundantCaseLabel
                case InteractionCommandError.UnknownCommand:
                // ReSharper disable once RedundantCaseLabel
                case InteractionCommandError.ConvertFailed:
                // ReSharper disable once RedundantCaseLabel
                case InteractionCommandError.UnmetPrecondition:
                default:
                    Log.Error("Command error: {Result}. Message content: {@MessageContent}", result.ToString(), context.Interaction);
                    Statistics.SlashCommandsFailed.WithLabels(command.Name).Inc();
                    break;
            }
        }
    }

    private async Task UserCommandExecuted(SocketInteraction socketInteraction)
    {
        Statistics.DiscordEvents.WithLabels(nameof(UserCommandExecuted)).Inc();

        if (socketInteraction is not SocketUserCommand socketUserCommand)
        {
            return;
        }

        var context = new SocketInteractionContext(_client, socketInteraction);
        var commandSearch = _interactionService.SearchUserCommand(socketUserCommand);

        if (!commandSearch.IsSuccess)
        {
            return;
        }
        
        var keepGoing = await CheckAttributes(context, commandSearch.Command.Attributes);

        if (!keepGoing)
        {
            return;
        }

        var result = await _interactionService.ExecuteCommandAsync(context, _provider);

        if (result.IsSuccess)
        {
            Statistics.UserCommandsExecuted.Inc();
        } else switch (result.Error)
        {
            case InteractionCommandError.ParseFailed:
            {
                Statistics.SlashCommandsFailed.WithLabels(commandSearch.Command.Name).Inc();
                var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
                embed.Embed.WithTitle("Error: Invalid input")
                    .WithDescription($"{result.ErrorReason}")
                    .WithColor(Color.Red);
                await context.SendResponse(_fergunInteractiveService, embed);
                break;
            }
            case InteractionCommandError.BadArgs:
            {
                Statistics.SlashCommandsFailed.WithLabels(commandSearch.Command.Name).Inc();
                var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
                embed.Embed.WithTitle("Error: Bad argument count")
                    .WithDescription($"You have provided too many or too few arguments for the command `{commandSearch.Command.Name}`")
                    .WithColor(Color.Red);
                await context.SendResponse(_fergunInteractiveService, embed);
                break;
            }
            case InteractionCommandError.Exception:
            {
                Statistics.SlashCommandsFailed.WithLabels(commandSearch.Command.Name).Inc();
                var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
                embed.Embed.WithTitle("Error: Exception")
                    .WithDescription($"An exception occurred while executing the command `{commandSearch.Command.Name}`\nDon't worry, the developers have been notified and will fix it as soon as possible")
                    .WithColor(Color.Red);
                await context.SendResponse(_fergunInteractiveService, embed);
                break;
            }
            case InteractionCommandError.Unsuccessful:
            {
                Statistics.SlashCommandsFailed.WithLabels(commandSearch.Command.Name).Inc();
                var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
                embed.Embed.WithTitle("Error: Unsuccessful")
                    .WithDescription($"The command `{commandSearch.Command.Name}` was unsuccessful. Don't worry, the developers have been notified and will fix it as soon as possible")
                    .WithColor(Color.Red);
                await context.SendResponse(_fergunInteractiveService, embed);
                break;
            }
            // TODO would be nice to log when one of these errors below gets hit
            // ReSharper disable once RedundantCaseLabel
            case null:
            // ReSharper disable once RedundantCaseLabel
            case InteractionCommandError.UnknownCommand:
            // ReSharper disable once RedundantCaseLabel
            case InteractionCommandError.ConvertFailed:
            // ReSharper disable once RedundantCaseLabel
            case InteractionCommandError.UnmetPrecondition:
            default:
                Log.Error("Command error: {Result}. Message content: {@MessageContent}", result.ToString(), context.Interaction); 
                Statistics.SlashCommandsFailed.WithLabels(commandSearch.Command.Name).Inc();
                break;
        }
    }

    private async Task AutoCompleteExecuted(SocketInteraction socketInteraction)
    {
        Statistics.DiscordEvents.WithLabels(nameof(AutoCompleteExecuted)).Inc();

        var context = new SocketInteractionContext(_client, socketInteraction);
        await _interactionService.ExecuteCommandAsync(context, _provider);

        Statistics.AutoCompletesExecuted.Inc();
    }

    private async Task SelectMenuExecuted(SocketInteraction socketInteraction)
    {
        Statistics.DiscordEvents.WithLabels(nameof(SelectMenuExecuted)).Inc();

        var context = new SocketInteractionContext(_client, socketInteraction);
        await _interactionService.ExecuteCommandAsync(context, _provider);

        Statistics.SelectMenusExecuted.Inc();
    }

    private async Task ModalSubmitted(SocketModal socketModal)
    {
        Statistics.DiscordEvents.WithLabels(nameof(ModalSubmitted)).Inc();

        var context = new SocketInteractionContext(_client, socketModal);
        await _interactionService.ExecuteCommandAsync(context, _provider);

        Statistics.ModalsExecuted.Inc();
    }

    private async Task ButtonExecuted(SocketMessageComponent socketMessageComponent)
    {
        Statistics.DiscordEvents.WithLabels(nameof(ButtonExecuted)).Inc();

        var context = new SocketInteractionContext(_client, socketMessageComponent);

        var commandSearch = _interactionService.SearchComponentCommand(socketMessageComponent);

        if (!commandSearch.IsSuccess)
        {
            return;
        }
        
        var keepGoing = await CheckAttributes(context, commandSearch.Command.Attributes);

        if (!keepGoing)
        {
            return;
        }

        await _interactionService.ExecuteCommandAsync(context, _provider);

        Statistics.ButtonExecuted.Inc();
    }
    
    private async Task<bool> CheckAttributes(SocketInteractionContext context, IReadOnlyCollection<Attribute>? attributes)
    {
        if (attributes == null)
        {
            return true;
        }
        if (attributes.OfType<GuildOnly>().Any())
        {
            if (context.Guild != null) return true;
            await context.Interaction.RespondAsync("This command is not supported in DMs.");
            context.LogCommandUsed(CommandResponse.NotSupportedInDm);
            return false;
        }

        return true;
    }
}