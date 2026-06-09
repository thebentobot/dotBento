using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Commands.SharedCommands;

namespace dotBento.Bot.ComponentInteractions;

public sealed class ServerSettingsComponentInteraction(SettingsCommand settingsCommand)
    : InteractionModuleBase<SocketInteractionContext>
{
    [ComponentInteraction("server-settings:leaderboard-public")]
    public async Task ToggleLeaderboardPublic()
    {
        var guildUser = Context.Guild.GetUser(Context.User.Id);
        if (guildUser is null || !guildUser.GuildPermissions.ManageGuild)
        {
            await RespondAsync("You need the **Manage Server** permission to change server settings.",
                ephemeral: true);
            return;
        }

        var response = await settingsCommand.ToggleLeaderboardPublicAsync(
            (long)Context.Guild.Id, Context.Guild.Name, Context.Guild.IconUrl);
        var component = (SocketMessageComponent)Context.Interaction;
        await component.UpdateAsync(m =>
        {
            m.Embed = response.Embed.Build();
            m.Components = response.Components?.Build();
        });
    }
}
