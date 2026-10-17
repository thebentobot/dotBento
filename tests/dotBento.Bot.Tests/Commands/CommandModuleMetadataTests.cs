using System.Reflection;
using Discord;
using Discord.Interactions;
using dotBento.Bot.Attributes;
using dotBento.Bot.Commands.SlashCommands;
using dotBento.Domain.Enums.Games;
using InteractionGroupAttribute = Discord.Interactions.GroupAttribute;

namespace dotBento.Bot.Tests.Commands;

public sealed class CommandModuleMetadataTests
{
    private static MethodInfo Method<T>(string name)
    {
        var method = typeof(T).GetMethod(name);
        Assert.NotNull(method);
        return method;
    }

    private static TAttribute Attribute<TAttribute>(MemberInfo member)
        where TAttribute : Attribute
    {
        var attribute = member.GetCustomAttribute<TAttribute>();
        Assert.NotNull(attribute);
        return attribute;
    }

    private static TAttribute Attribute<TAttribute>(System.Reflection.ParameterInfo parameter)
        where TAttribute : Attribute
    {
        var attribute = parameter.GetCustomAttribute<TAttribute>();
        Assert.NotNull(attribute);
        return attribute;
    }

    [Fact]
    public void ChooseSlashCommand_ExposesExpectedSlashMetadata()
    {
        var group = Attribute<InteractionGroupAttribute>(typeof(ChooseSlashCommand));
        var method = Method<ChooseSlashCommand>(nameof(ChooseSlashCommand.ChooseCommand));
        var command = Attribute<SlashCommandAttribute>(method);
        var parameters = method.GetParameters();

        Assert.Equal("choose", group.Name);
        Assert.Equal("Get help choosing something", group.Description);
        Assert.Equal("list", command.Name);
        Assert.Equal("Get Bento to choose from a list of options", command.Description);
        Assert.Equal("options", Attribute<Discord.Interactions.SummaryAttribute>(parameters[0]).Name);
        Assert.Equal(typeof(bool?), parameters[1].ParameterType);
    }

    [Fact]
    public void GameSlashCommand_ExposesExpectedGameCommands()
    {
        var group = Attribute<InteractionGroupAttribute>(typeof(GameSlashCommand));
        Assert.Equal("game", group.Name);

        var rps = Method<GameSlashCommand>(nameof(GameSlashCommand.RpsCommand));
        var eightBall = Method<GameSlashCommand>(nameof(GameSlashCommand.EightBallCommand));
        var roll = Method<GameSlashCommand>(nameof(GameSlashCommand.RollCommand));

        Assert.Equal("rps", Attribute<SlashCommandAttribute>(rps).Name);
        Assert.Equal(typeof(RpsGameChoice), rps.GetParameters()[0].ParameterType);
        Assert.Equal("8ball", Attribute<SlashCommandAttribute>(eightBall).Name);
        Assert.Equal("question", Attribute<Discord.Interactions.SummaryAttribute>(eightBall.GetParameters()[0]).Name);
        Assert.Equal("roll", Attribute<SlashCommandAttribute>(roll).Name);
        Assert.All(roll.GetParameters().Take(2), parameter => Assert.Equal(typeof(int?), parameter.ParameterType));
    }

    [Fact]
    public void WeatherSlashCommand_ExposesCheckSetAndDeleteCommands()
    {
        var group = Attribute<InteractionGroupAttribute>(typeof(WeatherSlashCommand));
        Assert.Equal("weather", group.Name);

        Assert.Equal("check", Attribute<SlashCommandAttribute>(Method<WeatherSlashCommand>(nameof(WeatherSlashCommand.UserCommand))).Name);
        Assert.Equal("set", Attribute<SlashCommandAttribute>(Method<WeatherSlashCommand>(nameof(WeatherSlashCommand.SetCommand))).Name);
        Assert.Equal("delete", Attribute<SlashCommandAttribute>(Method<WeatherSlashCommand>(nameof(WeatherSlashCommand.DeleteCommand))).Name);
    }

    [Fact]
    public void ServerSlashCommand_ExposesSettingsWithoutCommandsSubgroup()
    {
        var group = Attribute<InteractionGroupAttribute>(typeof(ServerSlashCommand));
        var settings = Method<ServerSlashCommand>(nameof(ServerSlashCommand.SettingsCommand));
        var slashCommand = Attribute<SlashCommandAttribute>(settings);
        var permission = Attribute<Discord.Interactions.RequireUserPermissionAttribute>(settings);

        Assert.Equal("server", group.Name);
        Assert.Equal("settings", slashCommand.Name);
        Assert.NotNull(settings.GetCustomAttribute<GuildOnly>());
        Assert.Equal(GuildPermission.ManageGuild, permission.GuildPermission);
        Assert.DoesNotContain(
            typeof(ServerSlashCommand).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic),
            type => type.GetCustomAttribute<InteractionGroupAttribute>()?.Name == "commands");
    }
}
