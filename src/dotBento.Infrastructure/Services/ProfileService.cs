using System.Text.Json;
using CSharpFunctionalExtensions;
using dotBento.EntityFramework.Context;
using dotBento.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace dotBento.Infrastructure.Services;

public sealed class ProfileService(IDistributedCache cache, IDbContextFactory<BotDbContext> contextFactory)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly DistributedCacheEntryOptions CacheEntryOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
    };

    public Task<Profile> CreateOrUpdateProfileAsync(long userId, Action<Profile>? applyChanges = null)
    {
        return UpsertProfileAsync(userId, applyChanges);
    }
    
    public Task<Profile> UpdateProfileAsync(long userId, Action<Profile> applyChanges)
    {
        return UpsertProfileAsync(userId, applyChanges);
    }

    private static string CacheKey(long userId) => $"profile:{userId}";

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
        
        var json = JsonSerializer.Serialize(profile, JsonOptions);
        await cache.SetStringAsync(CacheKey(profile.UserId), json, CacheEntryOptions);
        
        return profile;
    }
    
    public async Task<Maybe<Profile>> GetProfileAsync(long userId)
    {
        var key = CacheKey(userId);
        var cachedJson = await cache.GetStringAsync(key);
        if (!string.IsNullOrEmpty(cachedJson))
        {
            try
            {
                var cachedProfile = JsonSerializer.Deserialize<Profile>(cachedJson, JsonOptions);
                if (cachedProfile != null)
                    return cachedProfile;
            }
            catch
            {
                // ignore deserialization errors and fall back to DB
            }
        }
        
        await using var context = await contextFactory.CreateDbContextAsync();
        
        var profileEntity = await context.Profiles.FindAsync(userId);
        
        if (profileEntity == null)
        {
            return Maybe<Profile>.None;
        }
        
        var json = JsonSerializer.Serialize(profileEntity, JsonOptions);
        await cache.SetStringAsync(key, json, CacheEntryOptions);
        
        return profileEntity;
    }
}