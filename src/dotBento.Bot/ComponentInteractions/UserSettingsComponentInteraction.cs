using NetCord.Services.ComponentInteractions;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;

namespace dotBento.Bot.ComponentInteractions;

public sealed class UserSettingsComponentInteraction(SettingsCommand settingsCommand)
    : ComponentInteractionModule<ComponentInteractionContext>
{
    [ComponentInteraction("user-settings:hide-commands")]
    public async Task ToggleHideCommands()
    {
        var response = await settingsCommand.ToggleHideCommandsAsync((long)Context.User.Id);
        await Context.UpdateResponseAsync(response);
    }

    [ComponentInteraction("user-settings:global-leaderboard")]
    public async Task ToggleGlobalLeaderboard()
    {
        var response = await settingsCommand.ToggleGlobalLeaderboardAsync((long)Context.User.Id);
        await Context.UpdateResponseAsync(response);
    }
}
