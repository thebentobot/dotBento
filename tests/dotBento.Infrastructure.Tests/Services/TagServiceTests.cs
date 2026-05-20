using CSharpFunctionalExtensions;
using dotBento.EntityFramework.Context;
using dotBento.EntityFramework.Entities;
using dotBento.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace dotBento.Infrastructure.Tests.Services;

public sealed class TagServiceTests
{
    private sealed class InMemoryDbFactory : IDbContextFactory<BotDbContext>
    {
        private readonly string _dbName = Guid.NewGuid().ToString();
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

    [Fact]
    public async Task FindTagNamesForAutocompleteAsync_ReturnsAtMostTwentyFiveNames()
    {
        var factory = new InMemoryDbFactory();
        await SeedTagsAsync(factory, 1, 30);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new TagService(cache, factory);

        var result = await service.FindTagNamesForAutocompleteAsync(1, Maybe<long>.None, null);

        Assert.Equal(25, result.Count);
        Assert.Equal("tag-30-author-200", result[0]);
    }

    [Fact]
    public async Task FindTagNamesForAutocompleteAsync_FiltersByAuthorAndPrefix()
    {
        var factory = new InMemoryDbFactory();
        await SeedTagsAsync(factory, 1, 10);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new TagService(cache, factory);

        var result = await service.FindTagNamesForAutocompleteAsync(1, 200, "tag-");

        Assert.NotEmpty(result);
        Assert.All(result, name => Assert.Contains("-author-200", name));
    }

    private static async Task SeedTagsAsync(IDbContextFactory<BotDbContext> factory, long guildId, int count)
    {
        await using var db = await factory.CreateDbContextAsync();
        for (var i = 1; i <= count; i++)
        {
            var authorId = i % 2 == 0 ? 200 : 100;
            db.Tags.Add(new Tag
            {
                GuildId = guildId,
                UserId = authorId,
                Command = $"tag-{i:00}-author-{authorId}",
                Content = new string('x', 2000),
                Count = i,
                Date = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
    }
}
