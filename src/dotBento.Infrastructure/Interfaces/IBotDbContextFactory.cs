using dotBento.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;

namespace dotBento.Infrastructure.Interfaces;

public interface IBotDbContextFactory
{
    BotDbContext Create(DbContextOptions<BotDbContext> options);
}