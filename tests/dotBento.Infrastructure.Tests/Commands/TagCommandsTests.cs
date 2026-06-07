using CSharpFunctionalExtensions;
using dotBento.EntityFramework.Context;
using dotBento.EntityFramework.Entities;
using dotBento.Infrastructure.Commands;
using dotBento.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace dotBento.Infrastructure.Tests.Commands;

public sealed class TagCommandsTests
{
    private sealed class InMemoryDbFactory : IDbContextFactory<BotDbContext>
    {
        private readonly string _dbName = Guid.NewGuid().ToString("N");
        private readonly InMemoryDatabaseRoot _root = new();
        private readonly IConfiguration _config = new ConfigurationBuilder().Build();

        public BotDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<BotDbContext>()
                .UseInMemoryDatabase(_dbName, _root)
                .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
                .Options;
            return new BotDbContext(_config, options);
        }

        public Task<BotDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }

    private static TagCommands CreateSut(IDbContextFactory<BotDbContext> factory)
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new TagService(cache, factory);
        return new TagCommands(service);
    }

    private static async Task SeedTagAsync(IDbContextFactory<BotDbContext> factory, long guildId, string command, string content, long userId = 456, int count = 0)
    {
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        db.Tags.Add(new Tag
        {
            GuildId = guildId,
            UserId = userId,
            Command = command,
            Content = content,
            Count = count,
            Date = DateTime.UtcNow
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task FindTagsAsync_WhenGuildHasNoTags_ReturnsFailure()
    {
        var factory = new InMemoryDbFactory();
        var sut = CreateSut(factory);

        var result = await sut.FindTagsAsync(123, top: false, Maybe<long>.None);

        Assert.True(result.IsFailure);
        Assert.Equal("No tags found.", result.Error);
    }

    [Fact]
    public async Task FindTagNamesForAutocompleteAsync_FiltersByPrefixAndLimitsResults()
    {
        var factory = new InMemoryDbFactory();
        for (var i = 0; i < 30; i++)
        {
            await SeedTagAsync(factory, guildId: 123, command: $"Alpha{i:00}", content: "match");
        }
        await SeedTagAsync(factory, guildId: 123, command: "beta", content: "miss");
        var sut = CreateSut(factory);

        var result = await sut.FindTagNamesForAutocompleteAsync(123, "alpha", Maybe<long>.None);

        Assert.Equal(25, result.Count);
        Assert.All(result, tagName => Assert.StartsWith("Alpha", tagName, StringComparison.Ordinal));
    }

    [Fact]
    public async Task FindTagNamesForAutocompleteAsync_AppliesAuthorFilter()
    {
        var factory = new InMemoryDbFactory();
        await SeedTagAsync(factory, guildId: 123, command: "mine", content: "mine", userId: 456);
        await SeedTagAsync(factory, guildId: 123, command: "other", content: "other", userId: 789);
        var sut = CreateSut(factory);

        var result = await sut.FindTagNamesForAutocompleteAsync(123, null, 456L);

        Assert.Single(result);
        Assert.Equal("mine", result[0]);
    }

    [Fact]
    public async Task FindTagsAsync_CachesAuthorFilteredResultsSeparately()
    {
        var factory = new InMemoryDbFactory();
        await SeedTagAsync(factory, guildId: 123, command: "mine", content: "mine", userId: 456);
        await SeedTagAsync(factory, guildId: 123, command: "other", content: "other", userId: 789);
        var sut = CreateSut(factory);

        var allTags = await sut.FindTagsAsync(123, top: false, Maybe<long>.None);
        var authorTags = await sut.FindTagsAsync(123, top: false, 456L);

        Assert.True(allTags.IsSuccess);
        Assert.Equal(2, allTags.Value.Count);
        Assert.True(authorTags.IsSuccess);
        Assert.Single(authorTags.Value);
        Assert.Equal("mine", authorTags.Value[0].Command);
    }

    [Fact]
    public async Task SearchTagsAsync_WhenNoTagsMatch_ReturnsFailure()
    {
        var factory = new InMemoryDbFactory();
        await SeedTagAsync(factory, guildId: 123, command: "hello", content: "world");
        var sut = CreateSut(factory);

        var result = await sut.SearchTagsAsync(123, "missing");

        Assert.True(result.IsFailure);
        Assert.Equal("No tags found.", result.Error);
    }
}
