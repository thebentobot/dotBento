using dotBento.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;

namespace dotBento.WebApi.Tests;

/// <summary>
/// Test-only DbContext that carries the InMemory database metadata (name + shared root)
/// so multiple contexts can share the same store without relying on EF Core internal types.
/// </summary>
public sealed class TestBotDbContext : BotDbContext
{
    public string DatabaseName { get; }
    public InMemoryDatabaseRoot Root { get; }

    public TestBotDbContext(IConfiguration configuration,
        DbContextOptions<BotDbContext> options,
        string databaseName,
        InMemoryDatabaseRoot root) : base(configuration, options)
    {
        DatabaseName = databaseName;
        Root = root;
    }
}