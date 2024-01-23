using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Attributes;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Resources;
using dotBento.Bot.Services;
using dotBento.Domain;
using dotBento.Domain.Enums;
using Fergun.Interactive;
using Microsoft.Extensions.Caching.Memory;
using Prometheus;
using Serilog;

namespace dotBento.Bot.Handlers;

public class InteractionHandler
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactionService;
    private readonly InteractiveService _fergunInteractiveService;
    private readonly IServiceProvider _provider;
    private readonly UserService _userService;
    private readonly GuildService _guildService;

    private readonly IMemoryCache _cache;

    public InteractionHandler(DiscordSocketClient client,
        InteractionService interactionService,
        IServiceProvider provider,
        UserService userService,
        GuildService guildService,
        IMemoryCache cache,
        InteractiveService fergunInteractiveService)
    {
        _client = client;
        _interactionService = interactionService;
        _provider = provider;
        _userService = userService;
        _guildService = guildService;
        _cache = cache;
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
            var contextUser = await _userService.GetUserAsync(context.User.Id);

            var commandSearch = _interactionService.SearchSlashCommand(socketSlashCommand);

            if (!commandSearch.IsSuccess)
            {
                Log.Error("Someone tried to execute a non-existent slash command! {slashCommand}",
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
                _ = Task.Run(() => _userService.AddUserSlashCommandInteraction(context, command.Name));
                return;
            } else switch (result.Error)
            {
                case InteractionCommandError.ParseFailed:
                {
                    Statistics.CommandsFailed.WithLabels(command.Name).Inc();
                    var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
                    embed.Embed.WithTitle("Error: Invalid input")
                        .WithDescription($"{result.ErrorReason}")
                        .WithColor(Color.Red);
                    await context.SendResponse(_fergunInteractiveService, embed);
                    break;
                }
                case InteractionCommandError.BadArgs:
                {
                    Statistics.CommandsFailed.WithLabels(command.Name).Inc();
                    var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
                    embed.Embed.WithTitle("Error: Bad argument count")
                        .WithDescription($"You have provided too many or too few arguments for the command `{command.Name}`")
                        .WithColor(Color.Red);
                    await context.SendResponse(_fergunInteractiveService, embed);
                    break;
                }
                case InteractionCommandError.Exception:
                {
                    Statistics.CommandsFailed.WithLabels(command.Name).Inc();
                    var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
                    embed.Embed.WithTitle("Error: Exception")
                        .WithDescription($"An exception occurred while executing the command `{command.Name}`\nDon't worry, the developers have been notified and will fix it as soon as possible")
                        .WithColor(Color.Red);
                    await context.SendResponse(_fergunInteractiveService, embed);
                    break;
                }
                case InteractionCommandError.Unsuccessful:
                {
                    Statistics.CommandsFailed.WithLabels(command.Name).Inc();
                    var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
                    embed.Embed.WithTitle("Error: Unsuccessful")
                        .WithDescription($"The command `{command.Name}` was unsuccessful. Don't worry, the developers have been notified and will fix it as soon as possible")
                        .WithColor(Color.Red);
                    await context.SendResponse(_fergunInteractiveService, embed);
                    break;
                }
                // TODO would be nice to log when one of these errors below gets hit
                case null:
                case InteractionCommandError.UnknownCommand:
                case InteractionCommandError.ConvertFailed:
                case InteractionCommandError.UnmetPrecondition:
                default:
                    Log.Error(result.ToString() ?? "Command error (null)", context.Interaction);
                    Statistics.CommandsFailed.WithLabels(command.Name).Inc();
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
                Statistics.CommandsFailed.WithLabels(commandSearch.Command.Name).Inc();
                var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
                embed.Embed.WithTitle("Error: Invalid input")
                    .WithDescription($"{result.ErrorReason}")
                    .WithColor(Color.Red);
                await context.SendResponse(_fergunInteractiveService, embed);
                break;
            }
            case InteractionCommandError.BadArgs:
            {
                Statistics.CommandsFailed.WithLabels(commandSearch.Command.Name).Inc();
                var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
                embed.Embed.WithTitle("Error: Bad argument count")
                    .WithDescription($"You have provided too many or too few arguments for the command `{commandSearch.Command.Name}`")
                    .WithColor(Color.Red);
                await context.SendResponse(_fergunInteractiveService, embed);
                break;
            }
            case InteractionCommandError.Exception:
            {
                Statistics.CommandsFailed.WithLabels(commandSearch.Command.Name).Inc();
                var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
                embed.Embed.WithTitle("Error: Exception")
                    .WithDescription($"An exception occurred while executing the command `{commandSearch.Command.Name}`\nDon't worry, the developers have been notified and will fix it as soon as possible")
                    .WithColor(Color.Red);
                await context.SendResponse(_fergunInteractiveService, embed);
                break;
            }
            case InteractionCommandError.Unsuccessful:
            {
                Statistics.CommandsFailed.WithLabels(commandSearch.Command.Name).Inc();
                var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
                embed.Embed.WithTitle("Error: Unsuccessful")
                    .WithDescription($"The command `{commandSearch.Command.Name}` was unsuccessful. Don't worry, the developers have been notified and will fix it as soon as possible")
                    .WithColor(Color.Red);
                await context.SendResponse(_fergunInteractiveService, embed);
                break;
            }
            // TODO would be nice to log when one of these errors below gets hit
            case null:
            case InteractionCommandError.UnknownCommand:
            case InteractionCommandError.ConvertFailed:
            case InteractionCommandError.UnmetPrecondition:
            default:
                Log.Error(result.ToString() ?? "Command error (null)", context.Interaction);
                Statistics.CommandsFailed.WithLabels(commandSearch.Command.Name).Inc();
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
    
    private async Task<bool> CheckAttributes(SocketInteractionContext context, IReadOnlyCollection<Attribute> attributes)
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