using CSharpFunctionalExtensions;
using dotBento.EntityFramework.Context;
using dotBento.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dotBento.Infrastructure.Services;

public sealed class ProfileService(IMemoryCache cache, IDbContextFactory<BotDbContext> contextFactory)
{
    public Task<Profile> CreateOrUpdateProfileAsync(long userId, Action<Profile>? applyChanges = null)
    {
        return UpsertProfileAsync(userId, applyChanges);
    }
    
    public Task<Profile> UpdateProfileAsync(long userId, Action<Profile> applyChanges)
    {
        return UpsertProfileAsync(userId, applyChanges);
    }

    private async Task<Profile> UpsertProfileAsync(long userId, Action<Profile>? applyChanges)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
            
        var profile = await context.Profiles.FindAsync(userId) ?? new Profile { UserId = userId };

        applyChanges?.Invoke(profile);
        
        if (context.Entry(profile).State == EntityState.Detached)
        {
            context.Profiles.Add(profile);
        }
        else
        {
            context.Profiles.Update(profile);
        }
            
        await context.SaveChangesAsync();
        
        cache.Set($"profile:{profile.UserId}", profile);
        
        return profile;
    }
    
    public async Task<Maybe<Profile>> GetProfileAsync(long userId)
    {
        if (cache.TryGetValue($"profile:{userId}", out Profile? cached))
        {
            return cached;
        }
        
        await using var context = await contextFactory.CreateDbContextAsync();
        
        var profileEntity = await context.Profiles.FindAsync(userId);
        
        if (profileEntity == null)
        {
            return Maybe<Profile>.None;
        }
        
        cache.Set($"profile:{userId}", profileEntity);
        
        return profileEntity;
    }
}