using dotBento.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;

namespace dotBento.Domain.Interfaces;

public interface IBotDbContextFactory
{
    BotDbContext Create(DbContextOptions<BotDbContext> options);
}