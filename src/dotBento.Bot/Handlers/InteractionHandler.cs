using System.Reflection;
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

            if (!await CheckGuildOnly(context, slashCommand))
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

    /// <summary>
    /// Checks whether the slash command method is decorated with <see cref="GuildOnly"/> and,
    /// if so, ensures the interaction was sent from a guild. Returns false (and sends an
    /// ephemeral reply) when the command requires a guild but <c>context.Guild</c> is null.
    /// </summary>
    private async Task<bool> CheckGuildOnly(ApplicationCommandContext context, SlashCommandInteraction slashCommand)
    {
        if (context.Guild != null)
        {
            return true;
        }

        // Resolve the command name chain (e.g. "user profile" → parent "user", sub "profile")
        var commandName = slashCommand.Data.Name;
        var subCommandName = slashCommand.Data.Options?
            .FirstOrDefault(o => o.Type is ApplicationCommandOptionType.SubCommand
                                        or ApplicationCommandOptionType.SubCommandGroup)?.Name;

        // Search all registered ApplicationCommandModule types for the matching method
        var moduleTypes = Assembly.GetEntryAssembly()!
            .GetTypes()
            .Where(t => !t.IsAbstract && t.IsAssignableTo(typeof(ApplicationCommandModule<ApplicationCommandContext>)));

        foreach (var moduleType in moduleTypes)
        {
            // Check if this module is the top-level command
            var slashAttr = moduleType.GetCustomAttribute<SlashCommandAttribute>();
            if (slashAttr == null || !string.Equals(slashAttr.Name, commandName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (subCommandName == null)
            {
                // Top-level command — check the module class itself
                if (moduleType.GetCustomAttribute<GuildOnly>() != null)
                {
                    return await SendNotSupportedInDm(context);
                }
                return true;
            }

            // Check methods on this module for the sub-command
            foreach (var method in moduleType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                var subAttr = method.GetCustomAttribute<SubSlashCommandAttribute>();
                if (subAttr != null && string.Equals(subAttr.Name, subCommandName, StringComparison.OrdinalIgnoreCase))
                {
                    if (method.GetCustomAttribute<GuildOnly>() != null)
                    {
                        return await SendNotSupportedInDm(context);
                    }
                    return true;
                }
            }

            // Check nested classes (sub-command groups) for the sub-command
            foreach (var nestedType in moduleType.GetNestedTypes())
            {
                var nestedSlashAttr = nestedType.GetCustomAttribute<SubSlashCommandAttribute>();
                if (nestedSlashAttr == null)
                    continue;

                if (string.Equals(nestedSlashAttr.Name, subCommandName, StringComparison.OrdinalIgnoreCase))
                {
                    if (nestedType.GetCustomAttribute<GuildOnly>() != null)
                    {
                        return await SendNotSupportedInDm(context);
                    }

                    // Check methods within the nested group for a further sub-command
                    var subSubName = slashCommand.Data.Options?
                        .FirstOrDefault(o => o.Type is ApplicationCommandOptionType.SubCommand
                                                    or ApplicationCommandOptionType.SubCommandGroup)?
                        .Options?.FirstOrDefault(o => o.Type is ApplicationCommandOptionType.SubCommand)?.Name;

                    if (subSubName != null)
                    {
                        foreach (var method in nestedType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                        {
                            var subSubAttr = method.GetCustomAttribute<SubSlashCommandAttribute>();
                            if (subSubAttr != null && string.Equals(subSubAttr.Name, subSubName, StringComparison.OrdinalIgnoreCase))
                            {
                                if (method.GetCustomAttribute<GuildOnly>() != null)
                                {
                                    return await SendNotSupportedInDm(context);
                                }
                                return true;
                            }
                        }
                    }

                    return true;
                }
            }
        }

        return true;
    }

    private static async Task<bool> SendNotSupportedInDm(ApplicationCommandContext context)
    {
        await context.Interaction.SendResponseAsync(InteractionCallback.Message(
            new InteractionMessageProperties()
                .WithContent("This command is not supported in DMs.")
                .WithFlags(MessageFlags.Ephemeral)));
        context.LogCommandUsed(CommandResponse.NotSupportedInDm);
        return false;
    }
}
