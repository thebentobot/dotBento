using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;
using dotBento.Bot.Models;
using dotBento.Bot.Services;
using dotBento.Bot.Utilities;

namespace dotBento.CommandDocs;

public static class CommandManifestBuilder
{
    public static async Task<CommandManifest> BuildAsync(BotEnvConfig? config = null)
    {
        config ??= new();
        using var client = new DiscordSocketClient();
        using var interactions = new InteractionService(client);
        await CommandModules.LoadAsync(interactions, new MetadataServiceProvider(new BotPresentation(config)), config);

        var commands = interactions.SlashCommands
            .Select(BuildCommand)
            .OrderBy(command => command.Invocation, StringComparer.Ordinal)
            .ToArray();

        return new CommandManifest(1, commands);
    }

    private static CommandEntry BuildCommand(SlashCommandInfo command)
    {
        var modules = GetModules(command.Module);
        var groups = modules
            .Where(module => module.IsSlashGroup)
            .Select(module => new GroupEntry(module.SlashGroupName, module.Description ?? string.Empty))
            .ToArray();
        var path = groups.Select(group => group.Name).Append(command.Name).ToArray();
        var attributes = modules.SelectMany(module => module.Attributes).Concat(command.Attributes).ToArray();
        var preconditions = modules.SelectMany(module => module.Preconditions).Concat(command.Preconditions).ToArray();
        var permissions = preconditions
            .OfType<RequireUserPermissionAttribute>()
            .SelectMany(permission => new[] { permission.GuildPermission?.ToString(), permission.ChannelPermission?.ToString() })
            .Where(permission => permission is not null)
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .OrderBy(permission => permission, StringComparer.Ordinal)
            .ToArray();

        return new CommandEntry(
            SlashCommandIdUtilities.ResolveCommandId(command),
            path,
            "/" + string.Join(' ', path),
            command.Description,
            groups,
            attributes.OfType<GuildOnly>().Any(),
            permissions,
            command.Parameters.Select(BuildOption).ToArray());
    }

    private static OptionEntry BuildOption(SlashCommandParameterInfo option) => new(
        option.Name,
        option.Description,
        option.DiscordOptionType?.ToString() ?? option.ParameterType.Name,
        option.IsRequired,
        NormalizeValue(option.DefaultValue),
        GetChoices(option),
        option.IsAutocomplete,
        NormalizeLimit(option.MinValue),
        NormalizeLimit(option.MaxValue),
        option.MinLength,
        option.MaxLength,
        option.ChannelTypes.Select(type => type.ToString()).ToArray());

    private static IReadOnlyList<ChoiceEntry> GetChoices(SlashCommandParameterInfo option)
    {
        var properties = new ApplicationCommandOptionProperties
        {
            Choices = option.Choices.Select(choice => new ApplicationCommandOptionChoiceProperties
            {
                Name = choice.Name,
                Value = choice.Value
            }).ToList()
        };
        // Enum choices are populated during registration, rather than module discovery.
        option.TypeConverter.Write(properties, option);
        return properties.Choices?.Select(choice =>
            new ChoiceEntry(choice.Name, NormalizeValue(choice.Value) ?? string.Empty)).ToArray() ?? [];
    }

    private static IReadOnlyList<ModuleInfo> GetModules(ModuleInfo module)
    {
        var modules = new Stack<ModuleInfo>();
        for (ModuleInfo? current = module; current is not null; current = current.Parent)
            modules.Push(current);
        return modules.ToArray();
    }

    private static object? NormalizeValue(object? value) => value switch
    {
        null => null,
        Enum enumValue => enumValue.ToString(),
        string or bool or byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal => value,
        _ => value.ToString()
    };

    private static double? NormalizeLimit(double? value) =>
        value is -9007199254740991 or 9007199254740991 ? null : value;

    // Discord.Net constructs modules while reflecting them. The exporter never executes
    // a command, so inert uninitialised dependencies are sufficient and keep this tool
    // independent from databases, API keys, and the bot host.
    private sealed class MetadataServiceProvider(BotPresentation presentation) : IServiceProvider, IServiceScopeFactory, IServiceScope
    {
        public IServiceProvider ServiceProvider => this;
        public IServiceScope CreateScope() => this;
        public void Dispose() { }

        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(BotPresentation)) return presentation;
            if (serviceType == typeof(IServiceProvider) || serviceType == typeof(IServiceScopeFactory))
                return this;
            return serviceType.IsValueType || serviceType.IsInterface || serviceType.IsAbstract
                ? null
                : RuntimeHelpers.GetUninitializedObject(serviceType);
        }
    }
}
