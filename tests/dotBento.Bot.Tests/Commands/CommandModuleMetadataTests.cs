using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using dotBento.Bot.Attributes;
using dotBento.Bot.Commands.SlashCommands;
using dotBento.Bot.Commands.TextCommands;
using dotBento.Domain.Enums.Games;
using CommandAttribute = Discord.Commands.CommandAttribute;
using CommandRunMode = Discord.Commands.RunMode;
using InteractionGroupAttribute = Discord.Interactions.GroupAttribute;
using SummaryAttribute = Discord.Commands.SummaryAttribute;

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
    public void HoroscopeSlashCommand_ExposesAllPeriodsAndManagementCommands()
    {
        var group = Attribute<InteractionGroupAttribute>(typeof(HoroscopeSlashCommand));
        Assert.Equal("horoscope", group.Name);

        var names = typeof(HoroscopeSlashCommand)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(method => method.GetCustomAttribute<SlashCommandAttribute>()?.Name)
            .Where(name => name is not null)
            .ToHashSet();

        Assert.Equal(
            ["today", "yesterday", "tomorrow", "weekly", "monthly", "save", "remove", "list"],
            names);
        var todayParameters = Method<HoroscopeSlashCommand>(nameof(HoroscopeSlashCommand.TodayCommand))
            .GetParameters();
        Assert.Equal("sign", Attribute<Discord.Interactions.SummaryAttribute>(todayParameters[0]).Name);
        Assert.Equal("hide", Attribute<Discord.Interactions.SummaryAttribute>(todayParameters[1]).Name);
        Assert.True(todayParameters[0].HasDefaultValue);
        Assert.True(todayParameters[1].HasDefaultValue);
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

    [Fact]
    public void ChooseTextCommand_ExposesExpectedTextMetadata()
    {
        var moduleName = Attribute<NameAttribute>(typeof(ChooseTextCommand));
        var method = Method<ChooseTextCommand>(nameof(ChooseTextCommand.ChooseCommand));
        var command = Attribute<CommandAttribute>(method);
        var aliases = Attribute<AliasAttribute>(method);
        var examples = Attribute<ExamplesAttribute>(method);

        Assert.Equal("Choose", moduleName.Text);
        Assert.Equal("choose", command.Text);
        Assert.Equal(CommandRunMode.Async, command.RunMode);
        Assert.Equal(["pick"], aliases.Aliases);
        Assert.Equal(["choose option1, option2, option3"], examples.Examples);
        Assert.Equal("List of options to choose between", Attribute<SummaryAttribute>(method.GetParameters()[0]).Text);
    }

    [Fact]
    public void RollTextCommand_ExposesExpectedTextMetadata()
    {
        var method = Method<RollTextCommand>(nameof(RollTextCommand.RollCommand));
        var command = Attribute<CommandAttribute>(method);
        var examples = Attribute<ExamplesAttribute>(method);
        var parameters = method.GetParameters();

        Assert.Equal("roll", command.Text);
        Assert.Equal(CommandRunMode.Async, command.RunMode);
        Assert.Equal(["roll", "roll 1 10", "roll 1 1000"], examples.Examples);
        Assert.All(parameters, parameter => Assert.Equal(typeof(int?), parameter.ParameterType));
    }

    [Fact]
    public void WeatherTextCommand_ExposesExpectedTextMetadata()
    {
        var method = Method<WeatherTextCommand>(nameof(WeatherTextCommand.WeatherCommand));
        var command = Attribute<CommandAttribute>(method);
        var examples = Attribute<ExamplesAttribute>(method);
        var parameter = method.GetParameters().Single();

        Assert.Equal("weather", command.Text);
        Assert.Equal(CommandRunMode.Async, command.RunMode);
        Assert.Equal(["weather", "weather Copenhagen"], examples.Examples);
        Assert.NotNull(parameter.GetCustomAttribute<RemainderAttribute>());
        Assert.Equal(typeof(string), Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType);
    }

    [Fact]
    public void HoroscopeTextCommand_ExposesLegacyAliasesAndRemainderInput()
    {
        var method = Method<HoroscopeTextCommand>(nameof(HoroscopeTextCommand.HoroscopeCommand));
        var command = Attribute<CommandAttribute>(method);
        var aliases = Attribute<AliasAttribute>(method);

        Assert.Equal("horoscope", command.Text);
        Assert.Equal(CommandRunMode.Async, command.RunMode);
        Assert.Equal(["horo", "astro", "zodiac", "hs"], aliases.Aliases);
        Assert.NotNull(method.GetParameters().Single().GetCustomAttribute<RemainderAttribute>());
    }
}
