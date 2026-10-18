using Discord;
using dotBento.Bot.Commands.CommandHandlers;
using dotBento.Domain.Enums;
using dotBento.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace dotBento.Bot.Tests.Commands.CommandHandlers;

public sealed class BentoCommandTests
{
    [Theory]
    [InlineData(10L)]
    [InlineData(20L)]
    public async Task GiveBentoCommand_RejectsSuppressedUserBeforeWritingBento(long suppressedUserId)
    {
        var factory = new TestDbFactory();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var privacy = new PrivacyService(factory, cache);
        await privacy.OptOutAndDeleteAsync(suppressedUserId);
        var users = new UserService(cache, factory, privacyService: privacy);
        var command = new BentoCommand(new BentoService(cache, factory), new SupporterService(factory, cache), users);
        var sender = DiscordUser(10);
        var receiver = DiscordUser(20);

        var response = await command.GiveBentoCommand(sender, receiver);

        Assert.Equal(CommandResponse.WrongInput, response.CommandResponse);
        Assert.False(string.IsNullOrWhiteSpace(response.Embed.Build().Description));
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Empty(db.Bentos);
        Assert.DoesNotContain(db.Users, u => u.UserId == suppressedUserId);
    }

    private static IUser DiscordUser(ulong id)
    {
        var user = new Mock<IUser>();
        user.SetupGet(u => u.Id).Returns(id);
        user.SetupGet(u => u.Username).Returns($"User{id}");
        user.SetupGet(u => u.Discriminator).Returns("0001");
        return user.Object;
    }
}
