using System.Reflection;
using Discord.Interactions;
using dotBento.Bot.Attributes;
using dotBento.Bot.ComponentInteractions;

namespace dotBento.Bot.Tests.ComponentInteractions;

public sealed class UserSettingsComponentInteractionTests
{
    [Fact]
    public void ComponentHandlers_ExposeAllUserSettingsRoutes()
    {
        var routes = typeof(UserSettingsComponentInteraction)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Select(method => new
            {
                Method = method.Name,
                Attribute = method.GetCustomAttribute<ComponentInteractionAttribute>()
            })
            .Where(item => item.Attribute is not null)
            .ToDictionary(item => item.Method, item => item.Attribute!.CustomId);

        Assert.Equal(
            new Dictionary<string, string>
            {
                [nameof(UserSettingsComponentInteraction.ToggleHideCommands)] =
                    "user-settings:hide-commands",
                [nameof(UserSettingsComponentInteraction.ToggleGlobalLeaderboard)] =
                    "user-settings:global-leaderboard",
                [nameof(UserSettingsComponentInteraction.ShowDeleteDataConfirmation)] =
                    "user-settings:delete-data",
                [nameof(UserSettingsComponentInteraction.ConfirmDeleteData)] =
                    "user-settings:delete-data:confirm",
                [nameof(UserSettingsComponentInteraction.ShowSettings)] =
                    "user-settings:root",
                [nameof(UserSettingsComponentInteraction.OptBackIn)] =
                    "user-settings:opt-in"
            },
            routes);
    }

    [Fact]
    public void OptBackIn_IsExemptFromTheOptOutGate()
    {
        var method = typeof(UserSettingsComponentInteraction)
            .GetMethod(nameof(UserSettingsComponentInteraction.OptBackIn));

        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttribute<OptOutExempt>());
    }
}
