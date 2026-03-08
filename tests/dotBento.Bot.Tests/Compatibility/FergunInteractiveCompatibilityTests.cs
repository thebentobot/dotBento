using System.Reflection;
using Discord;

namespace dotBento.Bot.Tests.Compatibility;

/// <summary>
/// Verifies binary compatibility between Fergun.Interactive and the installed Discord.Net version.
///
/// Fergun.Interactive 1.9.1 was compiled against Discord.Net 3.18.0. If Discord.Net is upgraded
/// to a version where SelectMenuBuilder's constructor signature changes, the paginator throws a
/// MissingMethodException at runtime — breaking ALL multi-page paginator commands silently
/// (fire-and-forget callers) or visibly (awaited callers).
///
/// This test catches that at build/CI time rather than in production.
/// If it fails after a Discord.Net upgrade, check https://github.com/d4n3436/Fergun.Interactive
/// for a compatible release before proceeding with the upgrade.
/// </summary>
public class FergunInteractiveCompatibilityTests
{
    [Fact]
    public void SelectMenuBuilder_HasConstructorRequiredByFergunInteractive()
    {
        // The exact constructor Fergun.Interactive 1.9.1 calls, captured from the
        // MissingMethodException produced when Discord.Net was upgraded to 3.19.0:
        //
        // Void Discord.SelectMenuBuilder..ctor(String, List`1[SelectMenuOptionBuilder],
        //   String, Int32, Int32, Boolean, ComponentType, List`1[ChannelType],
        //   List`1[SelectMenuDefaultValue], Nullable`1[Int32])
        var expectedTypes = new[]
        {
            typeof(string),
            typeof(List<SelectMenuOptionBuilder>),
            typeof(string),
            typeof(int),
            typeof(int),
            typeof(bool),
            typeof(ComponentType),
            typeof(List<ChannelType>),
            typeof(List<SelectMenuDefaultValue>),
            typeof(int?),
        };

        var constructor = typeof(SelectMenuBuilder).GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: expectedTypes,
            modifiers: null);

        var discordNetVersion = typeof(SelectMenuBuilder).Assembly.GetName().Version;
        Assert.True(
            constructor is not null,
            $"Discord.Net {discordNetVersion} does not have the SelectMenuBuilder constructor " +
            "required by Fergun.Interactive 1.9.1. This will cause a MissingMethodException " +
            "and break all multi-page paginators at runtime. Either keep Discord.Net on 3.18.x " +
            "or upgrade Fergun.Interactive to a version that targets the new Discord.Net.");
    }
}
