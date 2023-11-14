using Discord.Commands;
using dotBento.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Services;

public class GuildService
{
    private readonly IDbContextFactory<BotDbContext> _contextFactory;
    private readonly IMemoryCache _cache;

    public GuildService(IDbContextFactory<BotDbContext> contextFactory, IMemoryCache cache)
    {
        this._contextFactory = contextFactory;
        this._cache = cache;
    }
    
    public bool CheckIfDM(ICommandContext context)
    {
        return context.Guild == null;
    }
    
    public async Task<EntityFramework.Entities.Guild?> GetGuildAsync(ulong? discordGuildId)
    {
        if (discordGuildId == null)
        {
            return null;
        }

        await using var db = await this._contextFactory.CreateDbContextAsync();
        return await db.Guilds
            .AsQueryable()
            .FirstOrDefaultAsync(f => f.GuildId == (long)discordGuildId);
    }

    public async Task<IDictionary<long, EntityFramework.Entities.GuildMember>> GetGuildUsers(
        ulong? discordGuildId = null)
    {
        if (discordGuildId == null)
        {
            return null;
        }

        await using var db = await this._contextFactory.CreateDbContextAsync();
        return await db.GuildMembers
            .AsQueryable()
            .Where(f => f.GuildId == (long)discordGuildId)
            .ToDictionaryAsync(f => f.UserId);
    }
    
    public async Task<EntityFramework.Entities.GuildMember?> GetGuildUserAsync(ulong? discordGuildId,
        ulong? discordUserId)
    {
        if (discordGuildId == null || discordUserId == null)
        {
            return null;
        }

        await using var db = await this._contextFactory.CreateDbContextAsync();
        return await db.GuildMembers
            .AsQueryable()
            .FirstOrDefaultAsync(f => f.GuildId == (long)discordGuildId && f.UserId == (long)discordUserId);
    }
    
    public async Task<EntityFramework.Entities.User?> GetUserFromGuildMemberAsync(ulong? discordUserId)
    {
        if (discordUserId == null)
        {
            return null;
        }

        await using var db = await this._contextFactory.CreateDbContextAsync();
        return await db.Users
            .AsQueryable()
            .FirstOrDefaultAsync(f => f.UserId == (long)discordUserId);
    }
    
    public async Task RemoveGuildAsync(ulong discordGuildId)
    {
        await using var db = await this._contextFactory.CreateDbContextAsync();
        var guild = await db.Guilds.AsQueryable().FirstOrDefaultAsync(f => f.GuildId == (long)discordGuildId);

        if (guild != null)
        {
            db.Guilds.Remove(guild);
            await db.SaveChangesAsync();
            await RemoveGuildFromCache(discordGuildId);
        }
    }
    
    public async Task<EntityFramework.Entities.Guild?> AddGuildAsync(ulong discordGuildId)
    {
        await using var db = await this._contextFactory.CreateDbContextAsync();
        var guild = await db.Guilds.AsQueryable().FirstOrDefaultAsync(f => f.GuildId == (long)discordGuildId);

        if (guild == null)
        {
            guild = new EntityFramework.Entities.Guild
            {
                GuildId = (long)discordGuildId,
                Prefix = "_"
                //Prefix = this._botSettings.DefaultPrefix
            };
            await db.Guilds.AddAsync(guild);
            await db.SaveChangesAsync();
            await AddGuildToCache(guild);
        }

        return guild;
    }
    
    private Task AddGuildToCache(EntityFramework.Entities.Guild guild)
    {
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(5));
        this._cache.Set(CacheKeyForGuild((ulong)guild.GuildId), guild, cacheEntryOptions);
        return Task.CompletedTask;
    }
    
    public async Task<EntityFramework.Entities.Guild?> UpdateGuildPrefixAsync(ulong discordGuildId, string prefix)
    {
        await using var db = await this._contextFactory.CreateDbContextAsync();
        var guild = await db.Guilds.AsQueryable().FirstOrDefaultAsync(f => f.GuildId == (long)discordGuildId);

        if (guild != null)
        {
            guild.Prefix = prefix;
            await db.SaveChangesAsync();
            await AddGuildToCache(guild);
        }

        return guild;
    }
    
    private Task RemoveGuildFromCache(ulong discordGuildId)
    {
        this._cache.Remove(CacheKeyForGuild(discordGuildId));
        return Task.CompletedTask;
    }

    private static string CacheKeyForGuild(ulong discordGuildId)
    {
        return $"guild-{discordGuildId}";
    }
}