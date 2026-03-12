using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;
using NetCord.Services.ComponentInteractions;
using dotBento.Bot.Attributes;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Domain;
using dotBento.Domain.Enums;
using dotBento.Bot.Services;
using dotBento.Infrastructure.Services;
using Fergun.Interactive;
using Prometheus;
using Serilog;

namespace dotBento.Bot.Handlers;

public sealed class InteractionHandler
{
    private readonly GatewayClient _client;
    private readonly ApplicationCommandService<ApplicationCommandContext, AutocompleteInteractionContext> _appCommands;
    private readonly ComponentInteractionService<ComponentInteractionContext> _componentCommands;
    private readonly ComponentInteractionService<ModalInteractionContext> _modalCommands;
    private readonly InteractiveService _fergunInteractiveService;
    private readonly IServiceProvider _provider;
    private readonly UserService _userService;
    private readonly GuildService _guildService;
    private readonly GuildMemberLookupService _memberLookup;

    public InteractionHandler(GatewayClient client,
        ApplicationCommandService<ApplicationCommandContext, AutocompleteInteractionContext> appCommands,
        ComponentInteractionService<ComponentInteractionContext> componentCommands,
        ComponentInteractionService<ModalInteractionContext> modalCommands,
        IServiceProvider provider,
        InteractiveService fergunInteractiveService,
        UserService userService,
        GuildService guildService,
        GuildMemberLookupService memberLookup)
    {
        _client = client;
        _appCommands = appCommands;
        _componentCommands = componentCommands;
        _modalCommands = modalCommands;
        _provider = provider;
        _fergunInteractiveService = fergunInteractiveService;
        _userService = userService;
        _guildService = guildService;
        _memberLookup = memberLookup;
        _client.InteractionCreate += InteractionCreated;
    }

    private ValueTask InteractionCreated(Interaction interaction)
    {
        switch (interaction)
        {
            case SlashCommandInteraction slashCommand:
                _ = Task.Run(() => ExecuteSlashCommand(slashCommand, _client));
                break;
            case UserCommandInteraction userCommand:
                _ = Task.Run(() => ExecuteUserCommand(userCommand, _client));
                break;
            case AutocompleteInteraction autocomplete:
                _ = Task.Run(() => ExecuteAutocomplete(autocomplete, _client));
                break;
            case ModalInteraction modal:
                _ = Task.Run(() => ExecuteModal(modal, _client));
                break;
            case MessageComponentInteraction component:
                _ = Task.Run(() => ExecuteComponent(component, _client));
                break;
        }
        return ValueTask.CompletedTask;
    }

    private async Task ExecuteSlashCommand(SlashCommandInteraction slashCommand, GatewayClient client)
    {
        Statistics.DiscordEvents.WithLabels("SlashCommandExecuted").Inc();

        using (Statistics.SlashCommandHandlerDuration.NewTimer())
        {
            var context = new ApplicationCommandContext(slashCommand, client);

            await EnsureGuildAndUserExists(context);

            var keepGoing = await CheckAttributes(context, slashCommand.Data.Options?.GetType().GetCustomAttributes(true).OfType<Attribute>().ToList());

            if (!keepGoing)
            {
                return;
            }

            var result = await _appCommands.ExecuteAsync(context, _provider);

            if (result is not IFailResult failResult)
            {
                Statistics.SlashCommandsExecuted.WithLabels(slashCommand.Data.Name).Inc();
                // TODO _ = Task.Run(() => _userService.AddUserSlashCommandInteraction(context, slashCommand.Data.Name));
            }
            else
            {
                Statistics.SlashCommandsFailed.WithLabels(slashCommand.Data.Name).Inc();
                Log.Error("Slash command error: {Error}. Command: {CommandName}", failResult.Message, slashCommand.Data.Name);
                try
                {
                    await context.Interaction.SendResponseAsync(InteractionCallback.Message(
                        new InteractionMessageProperties()
                            .WithContent($"An error occurred while executing the command: {failResult.Message}")
                            .WithFlags(MessageFlags.Ephemeral)));
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to send error response for slash command {CommandName}", slashCommand.Data.Name);
                }
            }
        }
    }

    private async Task ExecuteUserCommand(UserCommandInteraction userCommand, GatewayClient client)
    {
        Statistics.DiscordEvents.WithLabels("UserCommandExecuted").Inc();

        var context = new ApplicationCommandContext(userCommand, client);

        await EnsureGuildAndUserExists(context);

        var keepGoing = await CheckAttributes(context, null);

        if (!keepGoing)
        {
            return;
        }

        var result = await _appCommands.ExecuteAsync(context, _provider);

        if (result is not IFailResult failResult)
        {
            Statistics.UserCommandsExecuted.Inc();
        }
        else
        {
            Statistics.SlashCommandsFailed.WithLabels(userCommand.Data.Name).Inc();
            Log.Error("User command error: {Error}. Command: {CommandName}", failResult.Message, userCommand.Data.Name);
            try
            {
                await context.Interaction.SendResponseAsync(InteractionCallback.Message(
                    new InteractionMessageProperties()
                        .WithContent($"An error occurred while executing the command: {failResult.Message}")
                        .WithFlags(MessageFlags.Ephemeral)));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to send error response for user command {CommandName}", userCommand.Data.Name);
            }
        }
    }

    private async Task ExecuteAutocomplete(AutocompleteInteraction autocomplete, GatewayClient client)
    {
        Statistics.DiscordEvents.WithLabels("AutoCompleteExecuted").Inc();

        var ctx = new AutocompleteInteractionContext(autocomplete, client);
        await _appCommands.ExecuteAutocompleteAsync(ctx, _provider);

        Statistics.AutoCompletesExecuted.Inc();
    }

    private async Task ExecuteModal(ModalInteraction modal, GatewayClient client)
    {
        Statistics.DiscordEvents.WithLabels("ModalSubmitted").Inc();

        var ctx = new ModalInteractionContext(modal, client);
        await _modalCommands.ExecuteAsync(ctx, _provider);

        Statistics.ModalsExecuted.Inc();
    }

    private async Task ExecuteComponent(MessageComponentInteraction component, GatewayClient client)
    {
        Statistics.DiscordEvents.WithLabels("ButtonExecuted").Inc();

        var ctx = new ComponentInteractionContext(component, client);

        await _componentCommands.ExecuteAsync(ctx, _provider);

        Statistics.ButtonExecuted.Inc();
    }

    private async Task EnsureGuildAndUserExists(ApplicationCommandContext context)
    {
        if (context.Guild != null && !context.User.IsBot)
        {
            await _guildService.AddGuildAsync(context.Guild);
            await _userService.CreateOrAddUserToCache(context.User);
            var guildUser = await _memberLookup.GetOrFetchAsync(context.Guild.Id, context.User.Id, context.Guild);
            if (guildUser != null)
            {
                await _guildService.AddGuildMemberAsync(guildUser);
            }
        }
    }

    private async Task<bool> CheckAttributes(ApplicationCommandContext context, IReadOnlyCollection<Attribute>? attributes)
    {
        if (attributes == null)
        {
            return true;
        }
        if (attributes.OfType<GuildOnly>().Any())
        {
            if (context.Guild != null) return true;
            await context.Interaction.SendResponseAsync(InteractionCallback.Message(
                new InteractionMessageProperties()
                    .WithContent("This command is not supported in DMs.")
                    .WithFlags(MessageFlags.Ephemeral)));
            context.LogCommandUsed(CommandResponse.NotSupportedInDm);
            return false;
        }

        return true;
    }
}
