using CSharpFunctionalExtensions;
using dotBento.EntityFramework.Context;
using dotBento.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dotBento.Infrastructure.Services;

// NOTE: Profile caching is enabled. Cache is invalidated on updates in both bot service and Web API
// to ensure users immediately see their changes after editing their profile.
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
        
        // Invalidate cache for this user so subsequent reads fetch fresh data
        cache.Remove($"profile:{profile.UserId}");
        
        return profile;
    }
    
    public async Task<Maybe<Profile>> GetProfileAsync(long userId)
    {
        // Try cache first
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
        
        // Populate cache after successful fetch
        cache.Set($"profile:{userId}", profileEntity);
        
        return profileEntity;
    }
}