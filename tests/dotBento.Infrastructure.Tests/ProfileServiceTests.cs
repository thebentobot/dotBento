using System.Text.Json;
using CSharpFunctionalExtensions;
using dotBento.EntityFramework.Context;
using dotBento.EntityFramework.Entities;
using dotBento.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace dotBento.Infrastructure.Tests;

public class ProfileServiceTests
{
    private sealed class ThrowingFactory : IDbContextFactory<BotDbContext>
    {
        public BotDbContext CreateDbContext() => throw new InvalidOperationException("DB should not be used for this test");
        public Task<BotDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }

    private sealed class SharedInMemoryFactory : IDbContextFactory<BotDbContext>
    {
        private readonly string _dbName;
        private readonly InMemoryDatabaseRoot _root;
        private readonly IConfiguration _configuration;

        public SharedInMemoryFactory(string? dbName = null, InMemoryDatabaseRoot? root = null)
        {
            _dbName = dbName ?? Guid.NewGuid().ToString();
            _root = root ?? new InMemoryDatabaseRoot();
            _configuration = new ConfigurationBuilder().Build();
        }

        public BotDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<BotDbContext>()
                .UseInMemoryDatabase(_dbName, _root)
                .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
                .Options;
            return new BotDbContext(_configuration, options);
        }

        public Task<BotDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(CreateDbContext());
    }

    private static MemoryDistributedCache CreateMemoryCache()
        => new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

    private static async Task SeedUserAndProfileAsync(BotDbContext context, long userId, Action<Profile>? apply = null)
    {
        // InMemory provider does not enforce FK by default, but we add a User for realism
        context.Users.Add(new User
        {
            UserId = userId,
            Discriminator = "0001",
            Xp = 0,
            Level = 0,
            Username = "TestUser"
        });
        var profile = new Profile { UserId = userId };
        apply?.Invoke(profile);
        context.Profiles.Add(profile);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetProfileAsync_CacheHit_ReturnsCached_WithoutDbAccess()
    {
        // Arrange
        var cache = CreateMemoryCache();
        var profile = new Profile { UserId = 42, Description = "from-cache" };
        var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        await cache.SetStringAsync("profile:42", json);

        var factory = new ThrowingFactory();
        var service = new ProfileService(cache, factory);

        // Act
        var maybe = await service.GetProfileAsync(42);

        // Assert
        Assert.True(maybe.HasValue);
        Assert.Equal("from-cache", maybe.Value.Description);
    }

    [Fact]
    public async Task GetProfileAsync_CacheMiss_QueriesDb_And_PopulatesCache()
    {
        // Arrange DB with a profile
        var factory = new SharedInMemoryFactory();
        await using (var ctx = factory.CreateDbContext())
        {
            await SeedUserAndProfileAsync(ctx, 7, p => p.Description = "from-db");
        }

        var cache = CreateMemoryCache();
        var service = new ProfileService(cache, factory);

        // Act
        var maybe = await service.GetProfileAsync(7);

        // Assert
        Assert.True(maybe.HasValue);
        Assert.Equal("from-db", maybe.Value.Description);
        var cached = await cache.GetStringAsync("profile:7");
        Assert.False(string.IsNullOrEmpty(cached));
        var roundtrip = JsonSerializer.Deserialize<Profile>(cached!, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(roundtrip);
        Assert.Equal(7, roundtrip!.UserId);
        Assert.Equal("from-db", roundtrip.Description);
    }

    [Fact]
    public async Task GetProfileAsync_InvalidCachedJson_LogsWarning_FallsBackToDb_AndRefreshesCache()
    {
        // Arrange invalid cache
        var cache = CreateMemoryCache();
        await cache.SetStringAsync("profile:9", "{not-json}");

        var factory = new SharedInMemoryFactory();
        await using (var ctx = factory.CreateDbContext())
        {
            await SeedUserAndProfileAsync(ctx, 9, p => p.Description = "from-db");
        }

        var logger = new Mock<ILogger<ProfileService>>();
        var service = new ProfileService(cache, factory, logger.Object);

        // Act
        var maybe = await service.GetProfileAsync(9);

        // Assert result from DB
        Assert.True(maybe.HasValue);
        Assert.Equal("from-db", maybe.Value.Description);

        // Assert cache refreshed to valid JSON
        var cached = await cache.GetStringAsync("profile:9");
        Assert.NotEqual("{not-json}", cached);
        var parsed = JsonSerializer.Deserialize<Profile>(cached!, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(parsed);
        Assert.Equal(9, parsed!.UserId);

        // Verify a warning was logged
        logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Failed to deserialize Profile from cache")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ),
            Times.Once);
    }

    [Fact]
    public async Task CreateOrUpdate_PersistsAndCaches_NewProfile()
    {
        var factory = new SharedInMemoryFactory();
        var cache = CreateMemoryCache();
        var service = new ProfileService(cache, factory);

        // Act: create new profile
        var created = await service.CreateOrUpdateProfileAsync(100, p => p.Description = "hello");

        // Assert DB contains it
        await using (var verify = factory.CreateDbContext())
        {
            var dbProfile = await verify.Profiles.FindAsync(100L);
            Assert.NotNull(dbProfile);
            Assert.Equal("hello", dbProfile!.Description);
        }

        // Assert cache populated
        var cached = await cache.GetStringAsync("profile:100");
        Assert.False(string.IsNullOrEmpty(cached));
        var parsed = JsonSerializer.Deserialize<Profile>(cached!, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(parsed);
        Assert.Equal(100, parsed!.UserId);
        Assert.Equal("hello", parsed.Description);
    }

    [Fact]
    public async Task UpdateProfile_PersistsAndCaches_ExistingProfile()
    {
        var factory = new SharedInMemoryFactory();
        await using (var ctx = factory.CreateDbContext())
        {
            await SeedUserAndProfileAsync(ctx, 200, p => p.Description = "old");
        }

        var cache = CreateMemoryCache();
        var service = new ProfileService(cache, factory);

        // Act: update existing profile
        var updated = await service.UpdateProfileAsync(200, p => p.Description = "new");

        // Assert DB updated
        await using (var verify = factory.CreateDbContext())
        {
            var dbProfile = await verify.Profiles.FindAsync(200L);
            Assert.NotNull(dbProfile);
            Assert.Equal("new", dbProfile!.Description);
        }

        // Assert cache updated
        var cached = await cache.GetStringAsync("profile:200");
        var parsed = JsonSerializer.Deserialize<Profile>(cached!, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(parsed);
        Assert.Equal("new", parsed!.Description);
    }

    [Fact]
    public async Task CustomSerializerOptions_AreRespected_WhenCaching()
    {
        var factory = new SharedInMemoryFactory();
        var cache = CreateMemoryCache();
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            // Unlike the service default (WhenWritingNull), include nulls to validate override
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
        };
        var service = new ProfileService(cache, factory, serializerOptions: options);

        var profile = await service.CreateOrUpdateProfileAsync(300, p =>
        {
            p.Description = null; // ensure a null value exists
        });

        var json = await cache.GetStringAsync("profile:300");
        Assert.False(string.IsNullOrEmpty(json));
        Assert.Contains("\"description\":null", json!);
    }
}
