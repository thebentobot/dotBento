using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Services;
using dotBento.Domain;
using Microsoft.Extensions.Caching.Memory;
using Prometheus;
using Serilog;

namespace dotBento.Bot.Handlers;

public class InteractionHandler
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactionService;
    private readonly IServiceProvider _provider;
    private readonly UserService _userService;
    private readonly GuildService _guildService;

    private readonly IMemoryCache _cache;

    public InteractionHandler(DiscordSocketClient client,
        InteractionService interactionService,
        IServiceProvider provider,
        UserService userService,
        GuildService guildService,
        IMemoryCache cache)
    {
        _client = client;
        _interactionService = interactionService;
        _provider = provider;
        _userService = userService;
        _guildService = guildService;
        _cache = cache;
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

            await _interactionService.ExecuteCommandAsync(context, _provider);

            Statistics.SlashCommandsExecuted.WithLabels(command.Name).Inc();

            _ = Task.Run(() => _userService.AddUserSlashCommandInteraction(context, command.Name));
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

        await _interactionService.ExecuteCommandAsync(context, _provider);

        Statistics.UserCommandsExecuted.Inc();
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

        await _interactionService.ExecuteCommandAsync(context, _provider);

        Statistics.ButtonExecuted.Inc();
    }
}