using dotBento.Bot.Utilities;

namespace dotBento.Bot.Tests.Utilities;

public sealed class SlashCommandIdUtilitiesTests
{
    [Theory]
    [InlineData("server:settings")]
    [InlineData("SERVER:SETTINGS")]
    public void IsProtectedCommand_ServerSettings_IsProtectedCaseInsensitively(string commandId)
    {
        Assert.True(SlashCommandIdUtilities.IsProtectedCommand(commandId));
    }

    [Theory]
    [InlineData("server")]
    [InlineData("server:settings:child")]
    [InlineData("server:commands")]
    [InlineData("server:info")]
    [InlineData("tag:create")]
    public void IsProtectedCommand_NonSettingsIds_AreNotProtected(string commandId)
    {
        Assert.False(SlashCommandIdUtilities.IsProtectedCommand(commandId));
    }
}
