using dotBento.Bot.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Attributes;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Resources;
using dotBento.Bot.Utilities;
using dotBento.Domain;
using dotBento.Domain.Enums;
using dotBento.Infrastructure.Services;
using Fergun.Interactive;
using Prometheus;
using Serilog;

namespace dotBento.Bot.Handlers;

public sealed class InteractionHandler : IDisposable
{
    private readonly DiscordSocketClient _client;
    private readonly BotPresentation _presentation;
    private readonly InteractionService _interactionService;
    private readonly InteractiveService _fergunInteractiveService;
    private readonly IServiceProvider _provider;
    private readonly UserService _userService;
    private readonly GuildService _guildService;
    private readonly GuildSettingService _guildSettingService;
    private readonly PrivacyService? _privacyService;
    private readonly IDiscordGuildMemberResolver _guildMembers;

    public InteractionHandler(DiscordSocketClient client,
        InteractionService interactionService,
        IServiceProvider provider,
        InteractiveService fergunInteractiveService,
        UserService userService,
        GuildService guildService,
        GuildSettingService guildSettingService,
        IDiscordGuildMemberResolver guildMembers,
        BotPresentation? presentation = null,
        PrivacyService? privacyService = null)
    {
        _client = client;
        _presentation = presentation ?? new();
        _interactionService = interactionService;
        _provider = provider;
        _fergunInteractiveService = fergunInteractiveService;
        _userService = userService;
        _guildService = guildService;
        _guildSettingService = guildSettingService;
        _guildMembers = guildMembers;
        _privacyService = privacyService;
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

            if (await RejectDisabledAsync(socketInteraction)) return;

            var commandSearch = _interactionService.SearchSlashCommand(socketSlashCommand);

            if (!commandSearch.IsSuccess)
            {
                Log.Error("Someone tried to execute a non-existent slash command! {SlashCommand}",
                    socketSlashCommand.CommandName);
                return;
            }

            var command = commandSearch.Command;

            await EnsureGuildAndUserExists(context);

            var keepGoing = await CheckAttributes(context, commandSearch.Command);

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
                    var embed = new ResponseModel{ ResponseType = ResponseType.Embed, CommandResponse = CommandResponse.Error };
                    embed.Embed.WithTitle("Error: Invalid input")
                        .WithDescription($"{result.ErrorReason}")
                        .WithColor(_presentation.Error);
                    await context.SendResponse(_fergunInteractiveService, embed);
                    break;
                }
                case InteractionCommandError.BadArgs:
                {
                    Statistics.SlashCommandsFailed.WithLabels(command.Name).Inc();
                    var embed = new ResponseModel{ ResponseType = ResponseType.Embed, CommandResponse = CommandResponse.Error };
                    embed.Embed.WithTitle("Error: Bad argument count")
                        .WithDescription($"You have provided too many or too few arguments for the command `{command.Name}`")
                        .WithColor(_presentation.Error);
                    await context.SendResponse(_fergunInteractiveService, embed);
                    break;
                }
                case InteractionCommandError.Exception:
                {
                    Statistics.SlashCommandsFailed.WithLabels(command.Name).Inc();
                    Log.Error(result is ExecuteResult executeResult ? executeResult.Exception : null,
                        "Slash command {CommandName} failed for user {UserId} in guild {GuildId}: {ErrorReason}",
                        command.Name, context.User.Id, context.Guild?.Id, result.ErrorReason);
                    var embed = new ResponseModel{ ResponseType = ResponseType.Embed, CommandResponse = CommandResponse.Error };
                    embed.Embed.WithTitle("Error: Exception")
                        .WithDescription($"An exception occurred while executing the command `{command.Name}`\nDon't worry, the developers have been notified and will fix it as soon as possible.\n\n{_presentation.TroubleshootingHelp}")
                        .WithColor(_presentation.Error);
                    await context.SendResponse(_fergunInteractiveService, embed);
                    break;
                }
                case InteractionCommandError.Unsuccessful:
                {
                    Statistics.SlashCommandsFailed.WithLabels(command.Name).Inc();
                    var embed = new ResponseModel{ ResponseType = ResponseType.Embed, CommandResponse = CommandResponse.Error };
                    embed.Embed.WithTitle("Error: Unsuccessful")
                        .WithDescription($"The command `{command.Name}` was unsuccessful. Don't worry, the developers have been notified and will fix it as soon as possible.\n\n{_presentation.TroubleshootingHelp}")
                        .WithColor(_presentation.Error);
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

        await EnsureGuildAndUserExists(context);

        var keepGoing = await CheckAttributes(context, commandSearch.Command);

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
                var embed = new ResponseModel{ ResponseType = ResponseType.Embed, CommandResponse = CommandResponse.Error };
                embed.Embed.WithTitle("Error: Invalid input")
                    .WithDescription($"{result.ErrorReason}")
                    .WithColor(_presentation.Error);
                await context.SendResponse(_fergunInteractiveService, embed);
                break;
            }
            case InteractionCommandError.BadArgs:
            {
                Statistics.SlashCommandsFailed.WithLabels(commandSearch.Command.Name).Inc();
                var embed = new ResponseModel{ ResponseType = ResponseType.Embed, CommandResponse = CommandResponse.Error };
                embed.Embed.WithTitle("Error: Bad argument count")
                    .WithDescription($"You have provided too many or too few arguments for the command `{commandSearch.Command.Name}`")
                    .WithColor(_presentation.Error);
                await context.SendResponse(_fergunInteractiveService, embed);
                break;
            }
            case InteractionCommandError.Exception:
            {
                Statistics.SlashCommandsFailed.WithLabels(commandSearch.Command.Name).Inc();
                var embed = new ResponseModel{ ResponseType = ResponseType.Embed, CommandResponse = CommandResponse.Error };
                embed.Embed.WithTitle("Error: Exception")
                    .WithDescription($"An exception occurred while executing the command `{commandSearch.Command.Name}`\nDon't worry, the developers have been notified and will fix it as soon as possible.\n\n{_presentation.TroubleshootingHelp}")
                    .WithColor(_presentation.Error);
                await context.SendResponse(_fergunInteractiveService, embed);
                break;
            }
            case InteractionCommandError.Unsuccessful:
            {
                Statistics.SlashCommandsFailed.WithLabels(commandSearch.Command.Name).Inc();
                var embed = new ResponseModel{ ResponseType = ResponseType.Embed, CommandResponse = CommandResponse.Error };
                embed.Embed.WithTitle("Error: Unsuccessful")
                    .WithDescription($"The command `{commandSearch.Command.Name}` was unsuccessful. Don't worry, the developers have been notified and will fix it as soon as possible.\n\n{_presentation.TroubleshootingHelp}")
                    .WithColor(_presentation.Error);
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

    private async Task<bool> RejectDisabledAsync(SocketInteraction interaction)
    {
        var id = interaction switch
        {
            SocketSlashCommand slash => slash.CommandName,
            SocketMessageComponent component => component.Data.CustomId,
            _ => string.Empty
        };
        if (!CommandModules.IsDisabled(id, _presentation.Config)) return false;
        await interaction.RespondAsync("This feature has been disabled by the bot operator.", ephemeral: true);
        return true;
    }

    private async Task EnsureGuildAndUserExists(SocketInteractionContext context)
    {
        if (context.Guild != null && !context.User.IsBot)
        {
            await _guildService.AddGuildAsync(context.Guild);
            await _userService.CreateOrAddUserToCache(context.User);
            var member = await context.ResolveMemberAsync(_guildMembers, context.User);
            if (member.IsSuccess && member.Value.HasValue)
                await _guildService.AddGuildMemberAsync(member.Value.Value);
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
        if (await RejectDisabledAsync(socketInteraction)) return;
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
        if (await RejectDisabledAsync(socketMessageComponent)) return;
        Statistics.DiscordEvents.WithLabels(nameof(ButtonExecuted)).Inc();

        var context = new SocketInteractionContext(_client, socketMessageComponent);

        var commandSearch = _interactionService.SearchComponentCommand(socketMessageComponent);

        if (!commandSearch.IsSuccess)
        {
            return;
        }
        
        var keepGoing = await CheckAttributes(context, commandSearch.Command);

        if (!keepGoing)
        {
            return;
        }

        await _interactionService.ExecuteCommandAsync(context, _provider);

        Statistics.ButtonExecuted.Inc();
    }
    
    private async Task<bool> IsOptedOutAsync(ulong userId) =>
        _privacyService is not null && await _privacyService.IsUserOptedOutAsync((long)userId);

    private async Task<bool> CheckAttributes(SocketInteractionContext context, ICommandInfo command)
    {
        if (!command.Attributes.OfType<OptOutExempt>().Any() && await IsOptedOutAsync(context.User.Id))
        {
            await context.Interaction.RespondAsync(
                "You have opted out of Bento, so your data is not stored and commands are unavailable. " +
                "Use `/user settings` to opt back in.",
                ephemeral: true);
            return false;
        }

        if (command.Attributes.OfType<GuildOnly>().Any() && context.Guild == null)
        {
            await context.Interaction.RespondAsync("This command is not supported in DMs.");
            context.LogCommandUsed(CommandResponse.NotSupportedInDm);
            return false;
        }

        if (context.Guild == null) return true;

        var commandId = SlashCommandIdUtilities.ResolveCommandId(command);

        if (SlashCommandIdUtilities.IsProtectedCommand(commandId)) return true;

        var permissions = await _guildSettingService.GetCommandPermissionsAsync((long)context.Guild.Id);

        if (permissions.Disabled.Contains(commandId, StringComparer.OrdinalIgnoreCase))
        {
            await context.Interaction.RespondAsync(
                $"The command `{_presentation.Brand.CommandPath(commandId).Replace(':', ' ')}` has been disabled in this server.", ephemeral: true);
            return false;
        }

        if (permissions.AdminOnly.Contains(commandId, StringComparer.OrdinalIgnoreCase))
        {
            if (!await GuildMemberPermissions.HasAsync(_guildMembers, context.Guild, context.User, GuildPermission.ManageGuild))
            {
                await context.Interaction.RespondAsync(
                    $"The command `{_presentation.Brand.CommandPath(commandId).Replace(':', ' ')}` can only be used by members with the **Manage Server** permission.", ephemeral: true);
                return false;
            }
        }

        return true;
    }

    public void Dispose()
    {
        _client.SlashCommandExecuted -= SlashCommandExecuted;
        _client.AutocompleteExecuted -= AutoCompleteExecuted;
        _client.SelectMenuExecuted -= SelectMenuExecuted;
        _client.ModalSubmitted -= ModalSubmitted;
        _client.UserCommandExecuted -= UserCommandExecuted;
        _client.ButtonExecuted -= ButtonExecuted;
    }
}
