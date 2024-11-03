using CSharpFunctionalExtensions;
using dotBento.EntityFramework.Context;
using dotBento.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;

namespace dotBento.Infrastructure.Services;

public sealed class LastFmService(IDbContextFactory<BotDbContext> contextFactory)
{
    public async Task SaveLastFmAsync(long userId, string lastFmUsername)
    {
        var context = await contextFactory.CreateDbContextAsync();
        var lastFm = context.Lastfms.SingleOrDefault(x => x.UserId == userId);
        if (lastFm is null)
        {
            lastFm = new Lastfm
            {
                UserId = userId,
                Lastfm1 = lastFmUsername
            };
            await context.Lastfms.AddAsync(lastFm);
        }
        else
        {
            lastFm.Lastfm1 = lastFmUsername;
        }
        await context.SaveChangesAsync();
    }
    
    public async Task DeleteLastFmAsync(long userId)
    {
        var context = await contextFactory.CreateDbContextAsync();
        var lastFm = context.Lastfms.SingleOrDefault(x => x.UserId == userId);
        if (lastFm is null) return;
        context.Lastfms.Remove(lastFm);
        await context.SaveChangesAsync();
    }
    
    public async Task<Maybe<Lastfm>> GetLastFmAsync(long userId)
    {
        var context = await contextFactory.CreateDbContextAsync();
        var lastFm = context.Lastfms.SingleOrDefault(x => x.UserId == userId).AsMaybe();

        return lastFm;
    }
}