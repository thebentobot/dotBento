using dotBento.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace dotBento.Bot.Factories;

public class BotDbContextFactory
{
    private readonly IConfiguration _configuration;

    public BotDbContextFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public BotDbContext Create(DbContextOptions<BotDbContext> options)
    {
        return new BotDbContext(_configuration, options);
    }
}
