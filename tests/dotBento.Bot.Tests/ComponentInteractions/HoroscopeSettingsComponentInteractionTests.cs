using Discord;
using Discord.Interactions;
using dotBento.Bot.Commands.CommandHandlers;
using dotBento.Bot.ComponentInteractions;
using dotBento.Bot.Models;
using dotBento.Infrastructure.Services;
using dotBento.Infrastructure.Services.Api;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace dotBento.Bot.Tests.ComponentInteractions;

public sealed class HoroscopeSettingsComponentInteractionTests
{
    [Fact]
    public async Task SettingsComponents_FirstTimeDmUserCanSaveAndRemoveSign()
    {
        var (module, interaction, factory) = CreateModule();

        await module.SetSign("10", ["sagittarius"]);

        var service = new HoroscopeService(factory);
        Assert.Equal("sagittarius", (await service.GetHoroscopeAsync(10)).Value.Sign);
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            var user = Assert.Single(db.Users);
            Assert.Equal(10, user.UserId);
            Assert.Equal("dm-user", user.Username);
            Assert.Equal("0", user.Discriminator);
        }
        var saveUpdate = Assert.Single(interaction.Invocations, call => call.Method.Name == "ModifyOriginalResponseAsync");
        var properties = new MessageProperties();
        Assert.IsType<Action<MessageProperties>>(saveUpdate.Arguments[0])(properties);
        Assert.Contains("Sagittarius", properties.Embed.Value.Description);
        Assert.NotEmpty(properties.Components.Value.Components);

        await module.RemoveSign("10");

        Assert.True((await service.GetHoroscopeAsync(10)).HasNoValue);
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
            Assert.Equal("dm-user", Assert.Single(db.Users).Username);
        Assert.Equal(2, interaction.Invocations.Count(call => call.Method.Name == "DeferAsync"));
        Assert.Equal(2, interaction.Invocations.Count(call => call.Method.Name == "ModifyOriginalResponseAsync"));
    }

    [Fact]
    public async Task SettingsComponents_RejectOtherUsersWithoutMutatingPreferences()
    {
        var (module, interaction, factory) = CreateModule();
        var service = new HoroscopeService(factory);
        await service.SaveHoroscopeAsync(10, "aries");
        await service.SaveHoroscopeAsync(20, "libra");

        await module.SetSign("20", ["taurus"]);
        await module.RemoveSign("20");

        Assert.Equal("aries", (await service.GetHoroscopeAsync(10)).Value.Sign);
        Assert.Equal("libra", (await service.GetHoroscopeAsync(20)).Value.Sign);
        Assert.DoesNotContain(interaction.Invocations, call => call.Method.Name == "ModifyOriginalResponseAsync");
        Assert.Equal(2, interaction.Invocations.Count(call => call.Method.Name == "RespondAsync"));
    }

    [Theory]
    [InlineData("not-a-sign")]
    [InlineData("99")]
    public async Task SettingsComponents_InvalidSelectionDoesNotOverwriteSavedSign(string input)
    {
        var (module, interaction, factory) = CreateModule();
        var service = new HoroscopeService(factory);
        await service.SaveHoroscopeAsync(10, "aries");

        await module.SetSign("10", [input]);

        Assert.Equal("aries", (await service.GetHoroscopeAsync(10)).Value.Sign);
        Assert.DoesNotContain(interaction.Invocations, call => call.Method.Name == "ModifyOriginalResponseAsync");
        Assert.Single(interaction.Invocations, call => call.Method.Name == "RespondAsync");
    }

    private static (HoroscopeSettingsComponentInteraction Module, Mock<IComponentInteraction> Interaction,
        TestDbFactory Factory) CreateModule()
    {
        var factory = new TestDbFactory();
        var command = new HoroscopeCommand(new HoroscopeService(factory),
            new BentoMediaServerService(new HttpClient()), Options.Create(new BotEnvConfig()));
        var module = new HoroscopeSettingsComponentInteraction(command,
            new UserService(new MemoryCache(new MemoryCacheOptions()), factory));
        var interaction = new Mock<IComponentInteraction>();
        var context = new Mock<IInteractionContext>();
        context.SetupGet(value => value.User).Returns(Mock.Of<IUser>(user =>
            user.Id == 10 && user.Username == "dm-user" && user.Discriminator == "0"));
        context.SetupGet(value => value.Interaction).Returns(interaction.Object);
        ((IInteractionModuleBase)module).SetContext(context.Object);
        return (module, interaction, factory);
    }
}
