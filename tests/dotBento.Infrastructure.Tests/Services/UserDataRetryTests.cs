using System.Data.Common;
using Discord;
using dotBento.EntityFramework.Context;
using dotBento.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace dotBento.Infrastructure.Tests.Services;

public sealed class UserDataRetryTests
{
    [Theory]
    [InlineData("create")]
    [InlineData("delete")]
    [InlineData("opt-in")]
    public async Task UserDataOperations_OpenPostgresConnectionInsideRetryStrategy(string operation)
    {
        var probe = new RetryScopeProbe();
        var options = new DbContextOptionsBuilder<BotDbContext>()
            .UseNpgsql("Host=127.0.0.1;Database=unused;Username=unused;Password=unused",
                npgsql => npgsql.EnableRetryOnFailure())
            .AddInterceptors(probe)
            .Options;
        var factory = new OptionsFactory(options);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var privacy = new PrivacyService(factory, cache);
        var user = new Mock<IUser>();
        user.SetupGet(u => u.Id).Returns(10);

        // Abort before opening a socket: this exercises the real provider and retry strategy,
        // and verifies that transaction creation itself is already within the retry scope.
        await Assert.ThrowsAsync<ProbeCompletedException>(async () =>
        {
            switch (operation)
            {
                case "create":
                    await new UserService(cache, factory).CreateOrAddUserToCache(user.Object);
                    break;
                case "delete":
                    await privacy.OptOutAndDeleteAsync(10);
                    break;
                case "opt-in":
                    await privacy.RemoveOptOutAsync(10);
                    break;
            }
        });
        Assert.True(probe.ObservedRetryScope);
    }

    [Fact]
    public async Task CreateUser_RetriesWithFreshContextAndCachesOnlySuccessfulAttempt()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var saves = new FailFirstSave(cache);
        var options = new DbContextOptionsBuilder<BotDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ReplaceService<IExecutionStrategyFactory, RetryStrategyFactory>()
            .AddInterceptors(saves)
            .Options;
        var factory = new OptionsFactory(options);
        var user = new Mock<IUser>();
        user.SetupGet(u => u.Id).Returns(10);
        user.SetupGet(u => u.Username).Returns("User");
        user.SetupGet(u => u.Discriminator).Returns("0001");

        var result = await new UserService(cache, factory).CreateOrAddUserToCache(user.Object);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, saves.ContextIds.Count);
        Assert.NotEqual(saves.ContextIds[0], saves.ContextIds[1]);
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Equal("User", (await db.Users.SingleAsync(TestContext.Current.CancellationToken)).Username);
        Assert.True(cache.TryGetValue("user-10", out _));
    }

    private sealed class OptionsFactory(DbContextOptions<BotDbContext> options) : IDbContextFactory<BotDbContext>
    {
        public BotDbContext CreateDbContext() => new(new ConfigurationBuilder().Build(), options);
        public Task<BotDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }

    private sealed class ProbeCompletedException : Exception;

    private sealed class RetryScopeProbe : DbConnectionInterceptor
    {
        public bool ObservedRetryScope { get; private set; }

        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(
            DbConnection connection, ConnectionEventData eventData, InterceptionResult result,
            CancellationToken cancellationToken = default)
        {
            Assert.IsType<NpgsqlRetryingExecutionStrategy>(ExecutionStrategy.Current);
            ObservedRetryScope = true;
            throw new ProbeCompletedException();
        }
    }

    private sealed class RetryStrategyFactory(ExecutionStrategyDependencies dependencies) : IExecutionStrategyFactory
    {
        public IExecutionStrategy Create() => new NpgsqlRetryingExecutionStrategy(dependencies, 1, TimeSpan.Zero, null);
    }

    private sealed class FailFirstSave(IMemoryCache cache) : SaveChangesInterceptor
    {
        public List<Guid> ContextIds { get; } = [];

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Assert.IsType<NpgsqlRetryingExecutionStrategy>(ExecutionStrategy.Current);
            Assert.False(cache.TryGetValue("user-10", out _));
            ContextIds.Add(eventData.Context!.ContextId.InstanceId);
            if (ContextIds.Count == 1)
                throw new TimeoutException("Simulated transient database failure before saving.");
            return ValueTask.FromResult(result);
        }
    }
}
