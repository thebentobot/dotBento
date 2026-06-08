using NetCord;
using NetCord.Services.ComponentInteractions;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using dotBento.Bot.Services;

namespace dotBento.Bot.ComponentInteractions;

public sealed class ServerSettingsComponentInteraction(SettingsCommand settingsCommand, GuildMemberLookupService memberLookup)
    : ComponentInteractionModule<ComponentInteractionContext>
{
    [ComponentInteraction("server-settings:leaderboard-public")]
    public async Task ToggleLeaderboardPublic()
    {
        var guildUser = Context.Guild is not null
            ? await memberLookup.GetOrFetchAsync(Context.Guild.Id, Context.User.Id, Context.Guild)
            : null;
        if (guildUser is null || Context.Guild is null || !guildUser.HasGuildPermission(Context.Guild, Permissions.ManageGuild))
        {
            await Context.EphemeralResponseAsync("You need the **Manage Server** permission to change server settings.");
            return;
        }

        var iconUrl = Context.Guild?.IconHash != null
            ? $"https://cdn.discordapp.com/icons/{Context.Guild.Id}/{Context.Guild.IconHash}.png"
            : null;
        var response = await settingsCommand.ToggleLeaderboardPublicAsync(
            (long)Context.Guild!.Id, Context.Guild.Name, iconUrl);
        await Context.UpdateResponseAsync(response);
    }
}
