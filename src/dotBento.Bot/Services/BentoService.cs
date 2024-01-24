using CSharpFunctionalExtensions;
using dotBento.EntityFramework.Context;
using dotBento.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dotBento.Bot.Services;

public class BentoService(
    IMemoryCache cache,
    IDbContextFactory<BotDbContext> contextFactory)
{
    public async Task<Bento> FindOrCreateBentoAsync(long userId, int? amount)
    {
        if (cache.TryGetValue<Bento>(userId, out var bento))
        {
            return bento;
        }

        await using var context = await contextFactory.CreateDbContextAsync();
        bento = await context.Bentos.FirstOrDefaultAsync(x => x.UserId == userId);
        if (bento == null)
        {
            bento = new Bento
            {
                UserId = userId,
                Bento1 = amount ?? 0,
                BentoDate = DateTime.UtcNow,
            };
            await context.Bentos.AddAsync(bento);
            await context.SaveChangesAsync();
        }

        cache.Set(userId, bento, TimeSpan.FromMinutes(5));
        return bento;
    }
    
    public async Task<Maybe<Bento>> FindBentoAsync(long userId)
    {
        if (cache.TryGetValue<Bento>(userId, out var bento))
        {
            return bento;
        }

        await using var context = await contextFactory.CreateDbContextAsync();
        bento = await context.Bentos.FirstOrDefaultAsync(x => x.UserId == userId);
        if (bento == null)
        {
            return Maybe<Bento>.None;
        }

        cache.Set(userId, bento, TimeSpan.FromMinutes(5));
        return bento.AsMaybe();
    }
    
    public async Task<Bento> CreateBentoSenderAsync(long userId, DateTime bentoDate)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var bento = new Bento
        {
            UserId = userId,
            Bento1 = 0,
            BentoDate = bentoDate,
        };
        await context.Bentos.AddAsync(bento);
        await context.SaveChangesAsync();
        cache.Set(userId, bento, TimeSpan.FromMinutes(5));
        return bento;
    }
    
    public async Task IncrementBentoAsync(long userId, int amount)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var bento = await context.Bentos.FirstOrDefaultAsync(x => x.UserId == userId);
        if (bento == null)
        {
            bento = new Bento
            {
                UserId = userId,
                Bento1 = amount,
                BentoDate = DateTime.UtcNow,
            };
            await context.Bentos.AddAsync(bento);
        }
        else
        {
            bento.Bento1 += amount;
        }
        await context.SaveChangesAsync();
        cache.Set(userId, bento, TimeSpan.FromMinutes(5));
    }
    
    public async Task<Bento> UpsertBentoAsync(long userId, int amount)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var bento = await context.Bentos.FirstOrDefaultAsync(x => x.UserId == userId);
        if (bento == null)
        {
            bento = new Bento
            {
                UserId = userId,
                Bento1 = amount,
                BentoDate = DateTime.UtcNow.AddHours(-12),
            };
            await context.Bentos.AddAsync(bento);
        }
        else
        {
            bento.Bento1 += amount;
        }
        await context.SaveChangesAsync();
        cache.Set(userId, bento, TimeSpan.FromMinutes(5));
        return bento;
    }
    
    public async Task UpdateBentoDateAsync(long userId, DateTime bentoDate)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var bento = await context.Bentos.FirstOrDefaultAsync(x => x.UserId == userId);
        if (bento == null)
        {
            bento = new Bento
            {
                UserId = userId,
                Bento1 = 0,
                BentoDate = bentoDate,
            };
            await context.Bentos.AddAsync(bento);
        }
        else
        {
            bento.BentoDate = bentoDate;
        }
        await context.SaveChangesAsync();
        cache.Set(userId, bento, TimeSpan.FromMinutes(5));
    }
}