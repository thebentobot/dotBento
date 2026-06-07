using System.Reflection;
using dotBento.Bot.ComponentInteractions;
using dotBento.Bot.Services;
using NetCord.Services.ApplicationCommands;
using NetCord.Services.ComponentInteractions;

namespace dotBento.Bot.Tests.Services;

public sealed class BotServiceModuleRegistrationTests
{
    [Fact]
    public void RegisterInteractionModules_LoadsComponentModules()
    {
        var applicationCommands = new ApplicationCommandService<ApplicationCommandContext, AutocompleteInteractionContext>();
        var components = new ComponentInteractionService<ComponentInteractionContext>(
            ComponentInteractionServiceConfiguration<ComponentInteractionContext>.Default with
            {
                ParameterSeparator = '|'
            });
        var modals = new ComponentInteractionService<ModalInteractionContext>(
            ComponentInteractionServiceConfiguration<ModalInteractionContext>.Default with
            {
                ParameterSeparator = '|'
            });

        BotService.RegisterInteractionModules(
            applicationCommands,
            components,
            modals,
            typeof(UserSettingsComponentInteraction).Assembly);

        var customIds = components.GetComponentInteractions()
            .Select(interaction => GetProperty<ReadOnlyMemory<char>>(interaction, "Key").ToString())
            .ToHashSet();

        Assert.Contains("user-settings:hide-commands", customIds);
        Assert.Contains("user-settings:global-leaderboard", customIds);
        Assert.Contains("server-settings:leaderboard-public", customIds);
    }

    private static T GetProperty<T>(object instance, string name)
    {
        var property = instance.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"{instance.GetType().FullName} does not expose {name}.");
        return (T)property.GetValue(instance)!;
    }
}
