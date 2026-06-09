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

    [Fact]
    public async Task CreateFindUpdateRenameIncrementAndDeleteTagAsync_PersistsChanges()
    {
        var factory = new InMemoryDbFactory();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new TagService(cache, factory);

        var created = await service.CreateTagAsync(100, 1, "hello", "world");
        var found = await service.FindTagAsync(1, "hello");
        await service.UpdateTagAsync(100, 1, "hello", "updated");
        await service.IncrementTagCountAsync(created.TagId);
        await service.RenameTagAsync(100, 1, "hello", "renamed");
        var oldName = await service.FindTagAsync(1, "hello");
        var renamed = await service.FindTagAsync(1, "renamed");
        await service.DeleteTagAsync(100, 1, "renamed");
        await service.DeleteTagAsync(100, 1, "missing");
        await service.UpdateTagAsync(100, 1, "missing", "ignored");
        await service.RenameTagAsync(100, 1, "missing", "ignored");
        await service.IncrementTagCountAsync(999);
        var deleted = await service.FindTagAsync(1, "renamed");

        Assert.Equal("hello", created.Command);
        Assert.True(found.HasValue);
        Assert.True(oldName.HasNoValue);
        Assert.True(renamed.HasValue);
        Assert.Equal("updated", renamed.Value.Content);
        Assert.Equal(1, renamed.Value.Count);
        Assert.True(deleted.HasNoValue);
    }

    [Fact]
    public async Task FindTagsAsync_OrdersAndFiltersTags()
    {
        var factory = new InMemoryDbFactory();
        await SeedTagsAsync(factory, 1, 3);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new TagService(cache, factory);

        var alphabetical = await service.FindTagsAsync(1, top: false, Maybe<long>.None);
        var top = await service.FindTagsAsync(1, top: true, Maybe<long>.None);
        var author = await service.FindTagsAsync(1, top: false, 200);

        Assert.True(alphabetical.IsSuccess);
        Assert.Equal(["tag-01-author-100", "tag-02-author-200", "tag-03-author-100"],
            alphabetical.Value.Select(t => t.Command));
        Assert.Equal(["tag-03-author-100", "tag-02-author-200", "tag-01-author-100"],
            top.Value.Select(t => t.Command));
        var onlyAuthor = Assert.Single(author.Value);
        Assert.Equal(200, onlyAuthor.UserId);
    }

    [Fact]
    public async Task GetRandomTagAsync_ReturnsNoneWhenEmptyAndTagWhenPresent()
    {
        var factory = new InMemoryDbFactory();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new TagService(cache, factory);

        var missing = await service.GetRandomTagAsync(100, 1);
        await SeedTagsAsync(factory, 1, 1);
        var found = await service.GetRandomTagAsync(100, 1);

        Assert.True(missing.HasNoValue);
        Assert.True(found.HasValue);
        Assert.Equal(1, found.Value.GuildId);
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
