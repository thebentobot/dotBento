using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Commands.SharedCommands;

namespace dotBento.Bot.ComponentInteractions;

public sealed class UserSettingsComponentInteraction(SettingsCommand settingsCommand)
    : InteractionModuleBase<SocketInteractionContext>
{
    [ComponentInteraction("user-settings:hide-commands")]
    public async Task ToggleHideCommands()
    {
        var response = await settingsCommand.ToggleHideCommandsAsync((long)Context.User.Id);
        var component = (SocketMessageComponent)Context.Interaction;
        await component.UpdateAsync(m =>
        {
            m.Embed = response.Embed.Build();
            m.Components = response.Components?.Build();
        });
    }

    [ComponentInteraction("user-settings:global-leaderboard")]
    public async Task ToggleGlobalLeaderboard()
    {
        var response = await settingsCommand.ToggleGlobalLeaderboardAsync((long)Context.User.Id);
        var component = (SocketMessageComponent)Context.Interaction;
        await component.UpdateAsync(m =>
        {
            m.Embed = response.Embed.Build();
            m.Components = response.Components?.Build();
        });
    }
}
