using dotBento.Domain.Interfaces;
using dotBento.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace dotBento.Bot.Factories;

public class BotDbContextFactory(IConfiguration configuration) : IBotDbContextFactory
{
    public BotDbContext Create(DbContextOptions<BotDbContext> options)
    {
        return new BotDbContext(configuration, options);
    }
}
