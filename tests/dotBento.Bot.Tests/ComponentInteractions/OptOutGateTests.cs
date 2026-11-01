using System.Runtime.CompilerServices;
using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Attributes;
using dotBento.Bot.Models;
using dotBento.Bot.Services;
using Microsoft.Extensions.DependencyInjection;

namespace dotBento.Bot.Tests.ComponentInteractions;

public sealed class OptOutGateTests
{
    [Fact]
    public async Task UserSettingsCommands_AreExemptFromTheOptOutGate()
    {
        using var client = new DiscordSocketClient();
        using var interactions = new InteractionService(client);
        await CommandModules.LoadAsync(interactions, new MetadataServiceProvider(), new BotEnvConfig());

        var slash = Assert.Single(interactions.SlashCommands,
            command => command.Name == "settings" && command.Module.SlashGroupName == "user");
        Assert.Contains(slash.Attributes, attribute => attribute is OptOutExempt);

        var component = Assert.Single(interactions.ComponentCommands,
            command => command.Name == "user-settings:opt-in");
        Assert.Contains(component.Attributes, attribute => attribute is OptOutExempt);
    }

    private sealed class MetadataServiceProvider : IServiceProvider, IServiceScopeFactory, IServiceScope
    {
        public IServiceProvider ServiceProvider => this;
        public IServiceScope CreateScope() => this;
        public void Dispose() { }

        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(BotPresentation)) return new BotPresentation();
            if (serviceType == typeof(IServiceProvider) || serviceType == typeof(IServiceScopeFactory))
                return this;
            if (serviceType == typeof(IDiscordGuildMemberResolver))
                return new DiscordGuildMemberResolver();
            return serviceType.IsValueType || serviceType.IsInterface || serviceType.IsAbstract
                ? null
                : RuntimeHelpers.GetUninitializedObject(serviceType);
        }
    }
}
