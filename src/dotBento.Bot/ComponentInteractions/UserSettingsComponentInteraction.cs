using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Attributes;
using dotBento.Bot.Commands.CommandHandlers;
using dotBento.Bot.Models.Discord;

namespace dotBento.Bot.ComponentInteractions;

public sealed class UserSettingsComponentInteraction(SettingsCommand settingsCommand)
    : InteractionModuleBase<SocketInteractionContext>
{
    [ComponentInteraction("user-settings:hide-commands")]
    public async Task ToggleHideCommands()
    {
        var response = await settingsCommand.ToggleHideCommandsAsync((long)Context.User.Id);
        await UpdateMessageAsync(response);
    }

    [ComponentInteraction("user-settings:global-leaderboard")]
    public async Task ToggleGlobalLeaderboard()
    {
        var response = await settingsCommand.ToggleGlobalLeaderboardAsync((long)Context.User.Id);
        await UpdateMessageAsync(response);
    }

    [ComponentInteraction("user-settings:delete-data")]
    public async Task ShowDeleteDataConfirmation()
    {
        var response = settingsCommand.GetDeleteDataConfirmationAsync();
        await UpdateMessageAsync(response);
    }

    [ComponentInteraction("user-settings:delete-data:confirm")]
    public async Task ConfirmDeleteData()
    {
        var response = await settingsCommand.DeleteUserDataAsync((long)Context.User.Id);
        await UpdateMessageAsync(response);
    }

    [ComponentInteraction("user-settings:root")]
    public async Task ShowSettings()
    {
        var response = await settingsCommand.GetUserSettingsAsync((long)Context.User.Id);
        await UpdateMessageAsync(response);
    }

    [ComponentInteraction("user-settings:opt-in")]
    [OptOutExempt]
    public async Task OptBackIn()
    {
        var response = await settingsCommand.OptBackInAsync((long)Context.User.Id);
        await UpdateMessageAsync(response);
    }

    private async Task UpdateMessageAsync(ResponseModel response)
    {
        var component = (SocketMessageComponent)Context.Interaction;
        await component.UpdateAsync(m =>
        {
            m.Embed = response.Embed.Build();
            m.Components = response.Components?.Build();
        });
    }
}
