using Discord;
using dotBento.Bot.Handlers;

namespace dotBento.Bot.Tests.Handlers;

public sealed class InteractionHandlerTests
{
    [Fact]
    public void HasManageGuildPermission_RequiresExistingUserWithManageGuildPermission()
    {
        var noPermissions = new GuildPermissions(0);
        var manageGuild = new GuildPermissions((ulong)GuildPermission.ManageGuild);

        Assert.False(InteractionHandler.HasManageGuildPermission(null));
        Assert.False(InteractionHandler.HasManageGuildPermission(noPermissions));
        Assert.True(InteractionHandler.HasManageGuildPermission(manageGuild));
    }
}
