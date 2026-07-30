using System.Reflection;
using Discord.Interactions;
using dotBento.Bot.ComponentInteractions;

namespace dotBento.Bot.Tests.ComponentInteractions;

public sealed class ServerSettingsComponentInteractionTests
{
    [Fact]
    public void ComponentHandlers_ExposeAllServerSettingsRoutes()
    {
        var routes = typeof(ServerSettingsComponentInteraction)
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
                [nameof(ServerSettingsComponentInteraction.ToggleLeaderboardPublic)] =
                    "server-settings:leaderboard-public",
                [nameof(ServerSettingsComponentInteraction.ShowServerSettings)] =
                    "server-settings:root",
                [nameof(ServerSettingsComponentInteraction.ShowCommandPermissions)] =
                    "server-settings:commands:overview",
                [nameof(ServerSettingsComponentInteraction.ShowCommandAction)] =
                    "server-settings:commands:page:*:*",
                [nameof(ServerSettingsComponentInteraction.UpdateCommandPermission)] =
                    "server-settings:commands:select:*:*"
            },
            routes);
    }
}
