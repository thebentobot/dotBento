using System.Reflection;
using dotBento.Bot.Commands.SlashCommands;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace dotBento.Bot.Tests.Commands;

public sealed class ApplicationCommandMetadataTests
{
    [Fact]
    public void DominantColour_AllowsUrlOrAttachment()
    {
        var service = new ApplicationCommandService<ApplicationCommandContext, AutocompleteInteractionContext>();
        service.AddModule<ToolsSlashCommand>();

        var tools = FindCommand(service.GetCommands(), "tools");
        var dominantColour = FindOption(tools, "dominantcolour");
        var parameters = GetOptions(dominantColour).ToDictionary(option => GetProperty<string>(option, "Name"));

        Assert.True(GetProperty<bool>(parameters["url"], "IsOptional"));
        Assert.True(GetProperty<bool>(parameters["attachment"], "IsOptional"));
    }

    [Fact]
    public async Task DominantColour_RestPayload_DoesNotRequireUrlOrAttachment()
    {
        var service = new ApplicationCommandService<ApplicationCommandContext, AutocompleteInteractionContext>();
        service.AddModule<ToolsSlashCommand>();

        var tools = FindCommand(service.GetCommands(), "tools");
        var rawTools = await GetRawValueAsync<SlashCommandProperties>(tools);
        var options = rawTools.Options ?? throw new InvalidOperationException("tools command has no options.");
        var dominantColour = options.Single(option => option.Name == "dominantcolour");
        var parameters = dominantColour.Options!.ToDictionary(option => option.Name);

        Assert.False(parameters["url"].Required);
        Assert.False(parameters["attachment"].Required);
    }

    [Fact]
    public void OptionalSlashParameters_DoNotRegisterAsRequired()
    {
        var service = new ApplicationCommandService<ApplicationCommandContext, AutocompleteInteractionContext>();
        service.AddModule<ToolsSlashCommand>();

        var tools = FindCommand(service.GetCommands(), "tools");
        var timezone = FindOption(tools, "timezone");
        var parameters = GetOptions(timezone).ToDictionary(option => GetProperty<string>(option, "Name"));

        Assert.False(GetProperty<bool>(parameters["timezone"], "IsOptional"));
        Assert.True(GetProperty<bool>(parameters["compare"], "IsOptional"));
        Assert.True(GetProperty<bool>(parameters["hide"], "IsOptional"));
    }

    [Fact]
    public void AllDefaultedSlashParameters_RegisterAsOptional()
    {
        var service = new ApplicationCommandService<ApplicationCommandContext, AutocompleteInteractionContext>();
        service.AddModules(typeof(ToolsSlashCommand).Assembly);

        var violations = EnumerateParameters(service.GetCommands())
            .Where(parameter =>
                HasOptionalAttribute(parameter) &&
                !GetProperty<bool>(parameter, "IsOptional"))
            .Select(parameter => GetProperty<string>(parameter, "Name"))
            .ToList();

        Assert.Empty(violations);
    }

    private static IReadOnlyList<object> GetOptions(object command)
    {
        var value = GetValue(command);
        var options = value.GetType().GetProperty("Options", BindingFlags.Public | BindingFlags.Instance)
            ?.GetValue(value) as System.Collections.IEnumerable
            ?? value.GetType().GetProperty("SubCommands", BindingFlags.Public | BindingFlags.Instance)
                ?.GetValue(value) as System.Collections.IEnumerable
            ?? value.GetType().GetProperty("Parameters", BindingFlags.Public | BindingFlags.Instance)
            ?.GetValue(value) as System.Collections.IEnumerable;
        return options?.Cast<object>().ToList() ?? [];
    }

    private static object FindCommand(IEnumerable<object> commands, string name)
    {
        var list = commands.ToList();
        var match = list.SingleOrDefault(command => GetProperty<string>(command, "Name") == name);
        if (match is not null)
        {
            return match;
        }

        throw new InvalidOperationException("Available commands: " + string.Join(", ", list.Select(Describe)));
    }

    private static object FindOption(object command, string name)
    {
        var options = GetOptions(command);
        var match = options.SingleOrDefault(option => GetName(option) == name);
        if (match is not null)
        {
            return match;
        }

        throw new InvalidOperationException(
            $"{GetProperty<string>(command, "Name")} options: " + string.Join(", ", options.Select(Describe)) +
            " properties: " + string.Join(", ", command.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(property => $"{property.Name}={FormatValue(property.GetValue(command))}")));
    }

    private static string Describe(object command)
    {
        var name = GetName(command);
        var options = string.Join("/", GetOptions(command).Select(GetName));
        return $"{command.GetType().Name}:{name}[{options}]";
    }

    private static IEnumerable<object> EnumerateParameters(IEnumerable<object> commands)
    {
        foreach (var command in commands)
        {
            foreach (var option in GetOptions(command))
            {
                var value = GetValue(option);
                if (value.GetType().Name.StartsWith("SlashCommandParameter", StringComparison.Ordinal))
                {
                    yield return option;
                }

                foreach (var parameter in EnumerateParameters([option]))
                {
                    yield return parameter;
                }
            }
        }
    }

    private static bool HasOptionalAttribute(object parameter)
    {
        var attributes = GetProperty<System.Collections.IEnumerable>(parameter, "Attributes");

        foreach (var item in attributes)
        {
            var values = item!.GetType().GetProperty("Value", BindingFlags.Public | BindingFlags.Instance)
                ?.GetValue(item) as System.Collections.IEnumerable;

            if (values?.Cast<object>().Any(attribute => attribute is System.Runtime.InteropServices.OptionalAttribute) == true)
            {
                return true;
            }
        }

        return false;
    }

    private static async Task<T> GetRawValueAsync<T>(object command)
    {
        var value = GetValue(command);
        var method = value.GetType().GetMethod(
            "GetRawValueAsync",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"{value.GetType().FullName} does not expose GetRawValueAsync.");

        var rawTask = method.Invoke(value, [CancellationToken.None])
            ?? throw new InvalidOperationException("GetRawValueAsync returned null.");
        dynamic awaitable = rawTask;
        return (T)await awaitable;
    }

    private static string GetName(object instance)
    {
        var key = instance.GetType().GetProperty("Key", BindingFlags.Public | BindingFlags.Instance)?.GetValue(instance);
        if (key is string keyText)
        {
            return keyText;
        }

        return GetProperty<string>(instance, "Name");
    }

    private static object GetValue(object instance)
    {
        return instance.GetType().GetProperty("Value", BindingFlags.Public | BindingFlags.Instance)?.GetValue(instance)
            ?? instance;
    }

    private static string FormatValue(object? value)
    {
        if (value is null)
        {
            return "<null>";
        }

        if (value is string text)
        {
            return text;
        }

        if (value is System.Collections.IEnumerable enumerable)
        {
            return "[" + string.Join("/", enumerable.Cast<object>().Select(item => item?.ToString())) + "]";
        }

        return value.ToString() ?? string.Empty;
    }

    private static T GetProperty<T>(object instance, string name)
    {
        var value = GetValue(instance);
        var property = value.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException(
                $"{value.GetType().FullName} does not expose {name}. Properties: " +
                string.Join(", ", value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => $"{p.Name}={FormatValue(p.GetValue(value))}")));
        return (T)property.GetValue(value)!;
    }
}
