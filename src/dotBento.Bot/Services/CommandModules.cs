using Discord.Interactions;
using dotBento.Bot.Commands.SlashCommands;
using dotBento.Bot.ComponentInteractions;
using dotBento.Bot.Models;
using dotBento.Bot.Utilities;

namespace dotBento.Bot.Services;

public static class CommandModules
{
    public static async Task LoadAsync(InteractionService interactions, IServiceProvider provider, BotEnvConfig config)
    {
        try
        {
            await interactions.AddModulesAsync(typeof(BotService).Assembly, provider);
        }
        catch (InvalidOperationException exception)
        {
            throw new ArgumentException("Could not load configured commands. Check for name collisions.", nameof(config), exception);
        }
        var invocations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var command in interactions.SlashCommands)
        {
            var path = SlashCommandIdUtilities.ResolveInvocationId(command);
            if (!invocations.Add(path)) throw new ArgumentException($"Configured command name collision: {path}");
            ValidateDescription(command.Description);
            for (var module = command.Module; module is not null; module = module.Parent)
                if (module.IsSlashGroup) ValidateDescription(module.Description);
        }
        // At every level, a name must identify either a command or a group.
        foreach (var invocation in invocations)
        {
            var segments = invocation.Split(':');
            for (var length = 1; length < segments.Length; length++)
                if (invocations.Contains(string.Join(':', segments.Take(length))))
                    throw new ArgumentException($"Configured command conflicts with a group: {invocation}");
        }
        if (!config.MediaEnabled)
            await interactions.RemoveModuleAsync<MediaSlashCommand>();
        if (!config.HoroscopeEnabled)
        {
            await interactions.RemoveModuleAsync<HoroscopeSlashCommand>();
            await interactions.RemoveModuleAsync<HoroscopeSettingsComponentInteraction>();
        }
    }

    private static void ValidateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description) || description.Length > 100)
            throw new ArgumentException("Configured command descriptions must contain 1–100 characters.");
    }

    public static bool IsDisabled(string rootOrCustomId, BotEnvConfig config) =>
        (!config.MediaEnabled && rootOrCustomId == "media") ||
        (!config.HoroscopeEnabled && (rootOrCustomId == "horoscope" ||
                                     rootOrCustomId.StartsWith("horoscope-settings:", StringComparison.Ordinal)));
}
