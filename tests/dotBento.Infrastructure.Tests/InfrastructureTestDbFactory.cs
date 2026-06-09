using dotBento.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;

namespace dotBento.Infrastructure.Tests;

internal sealed class InfrastructureTestDbFactory : IDbContextFactory<BotDbContext>
{
    private readonly string _dbName = Guid.NewGuid().ToString();
    private readonly InMemoryDatabaseRoot _root = new();
    private readonly IConfiguration _configuration = new ConfigurationBuilder().Build();

    public BotDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BotDbContext>()
            .UseInMemoryDatabase(_dbName, _root)
            .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
            .Options;

        return new BotDbContext(_configuration, options);
    }

    public Task<BotDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(CreateDbContext());
}
