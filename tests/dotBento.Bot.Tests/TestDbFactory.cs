using dotBento.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;

namespace dotBento.Bot.Tests;

internal sealed class TestDbFactory(string? dbName = null, InMemoryDatabaseRoot? root = null)
    : IDbContextFactory<BotDbContext>
{
    private readonly string _dbName = dbName ?? Guid.NewGuid().ToString("N");
    private readonly InMemoryDatabaseRoot _root = root ?? new InMemoryDatabaseRoot();
    private readonly IConfiguration _config = new ConfigurationBuilder().Build();

    public BotDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BotDbContext>()
            .UseInMemoryDatabase(_dbName, _root)
            .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new BotDbContext(_config, options);
    }

    public Task<BotDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CreateDbContext());
    }
}
