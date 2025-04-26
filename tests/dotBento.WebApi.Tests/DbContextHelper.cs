using dotBento.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace dotBento.WebApi.Tests;

public static class DbContextHelper
{
    public static BotDbContext GetInMemoryDbContext()
    {
        var configuration = new ConfigurationBuilder().Build(); // Empty config

        var options = new DbContextOptionsBuilder<BotDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BotDbContext(configuration, options);
    }
}