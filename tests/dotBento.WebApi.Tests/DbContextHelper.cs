using dotBento.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;

namespace dotBento.WebApi.Tests;

public static class DbContextHelper
{
    private static readonly InMemoryDatabaseRoot SharedRoot = new();

    public static BotDbContext GetInMemoryDbContext()
    {
        var configuration = new ConfigurationBuilder().Build(); // Empty config

        // Use a shared InMemoryDatabaseRoot with a named database to allow multiple contexts
        // to share the same store via public EF Core APIs.
        var dbName = Guid.NewGuid().ToString();
        var root = SharedRoot;

        var options = new DbContextOptionsBuilder<BotDbContext>()
            .UseInMemoryDatabase(dbName, root)
            .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
            .Options;

        return new TestBotDbContext(configuration, options, dbName, root);
    }
}