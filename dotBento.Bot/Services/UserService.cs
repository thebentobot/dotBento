using CSharpFunctionalExtensions;
using Discord;
using dotBento.Domain.Interfaces;
using dotBento.EntityFramework.Context;
using dotBento.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Services;

public class UserService
{
    private readonly IMemoryCache _cache;
    private readonly IDbContextFactory<BotDbContext> _contextFactory;
    private readonly IBotDbContextFactory _botDbContextFactory;

    public UserService(IMemoryCache cache,
        IDbContextFactory<BotDbContext> contextFactory,
        IBotDbContextFactory botDbContextFactory)
    {
        this._cache = cache;
        this._contextFactory = contextFactory;
        this._botDbContextFactory = botDbContextFactory;
    }

    public async Task<Maybe<User>> GetUserFromDatabaseAsync(ulong discordUserId)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var user = await db.Users
            .AsNoTracking()
            .FirstAsync(f => f.UserId == (long)discordUserId);

        return user.AsMaybe();
    }

    public Task<Maybe<User>> GetUserFromCache(ulong discordUserId)
    {
        var discordUserIdCacheKey = UserDiscordIdCacheKey((long)discordUserId);

        _cache.TryGetValue(discordUserIdCacheKey, out User user);

        return Task.FromResult(user.AsMaybe());
    }

    private void RemoveUserFromCache(User user)
    {
        this._cache.Remove(UserDiscordIdCacheKey(user.UserId));
    }

    public static string UserDiscordIdCacheKey(long discordUserId)
    {
        return $"user-{discordUserId}";
    }
    
    public async Task<Dictionary<long, User>> GetMultipleUsers(HashSet<int> userIds)
    {
        await using var db = await this._contextFactory.CreateDbContextAsync();
        return await db.Users
            .AsNoTracking()
            .Where(w => userIds.Contains((int)w.UserId))
            .ToDictionaryAsync(d => d.UserId, d => d);
    }
    
    public async Task<List<User>> GetAllDiscordUserIds()
    {
        await using var db = await this._contextFactory.CreateDbContextAsync();
        return await db.Users
            .AsNoTracking()
            .ToListAsync();
    }
    
    public static async Task<string> GetNameAsync(IGuild guild, IUser user)
    {
        if (guild == null)
        {
            return user.GlobalName ?? user.Username;
        }

        var guildUser = await guild.GetUserAsync(user.Id);

        return guildUser?.DisplayName ?? user.GlobalName ?? user.Username;
    }
    
    public async Task<int> GetTotalUserCountAsync()
    {
        await using var db = await this._contextFactory.CreateDbContextAsync();
        return await db.Users
            .AsQueryable()
            .CountAsync();
    }

    public async Task DeleteUserAsync(ulong discordUserId)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var user = await db.Users
            .AsQueryable()
            .FirstOrDefaultAsync(f => f.UserId == (long)discordUserId);

        if (user != null)
        {
            db.Users.Remove(user);
            await db.SaveChangesAsync();
            RemoveUserFromCache(user);
        }
    }
}