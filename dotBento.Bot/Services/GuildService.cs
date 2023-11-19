using CSharpFunctionalExtensions;
using Discord.Commands;
using dotBento.EntityFramework.Context;
using dotBento.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dotBento.Bot.Services;

public class GuildService
{
    private readonly IDbContextFactory<BotDbContext> _contextFactory;
    private readonly IMemoryCache _cache;

    public GuildService(IDbContextFactory<BotDbContext> contextFactory, IMemoryCache cache)
    {
        _contextFactory = contextFactory;
        _cache = cache;
    }
    
    public bool CheckIfDM(ICommandContext context)
    {
        return context.Guild == null;
    }
    
    public async Task<Maybe<Guild>> GetGuildAsync(ulong? discordGuildId)
    {
        if (discordGuildId == null)
        {
            return null;
        }

        await using var db = await _contextFactory.CreateDbContextAsync();
        return await db.Guilds
            .AsQueryable()
            .FirstOrDefaultAsync(f => f.GuildId == (long)discordGuildId);
    }

    public async Task<IDictionary<long, GuildMember>> GetGuildUsers(
        ulong? discordGuildId = null)
    {
        if (discordGuildId == null)
        {
            return null;
        }

        await using var db = await _contextFactory.CreateDbContextAsync();
        return await db.GuildMembers
            .AsQueryable()
            .Where(f => f.GuildId == (long)discordGuildId)
            .ToDictionaryAsync(f => f.UserId);
    }
    
    public async Task<Maybe<GuildMember>> GetGuildUserAsync(ulong? discordGuildId,
        ulong? discordUserId)
    {
        if (discordGuildId == null || discordUserId == null)
        {
            return null;
        }

        await using var db = await _contextFactory.CreateDbContextAsync();
        return await db.GuildMembers
            .AsQueryable()
            .FirstOrDefaultAsync(f => f.GuildId == (long)discordGuildId && f.UserId == (long)discordUserId);
    }
    
    public async Task<Maybe<User>> GetUserFromGuildMemberAsync(ulong? discordUserId)
    {
        if (discordUserId == null)
        {
            return null;
        }

        await using var db = await _contextFactory.CreateDbContextAsync();
        return await db.Users
            .AsQueryable()
            .FirstOrDefaultAsync(f => f.UserId == (long)discordUserId);
    }
    
    public async Task RemoveGuildAsync(ulong discordGuildId)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var guild = await db.Guilds.AsQueryable().FirstOrDefaultAsync(f => f.GuildId == (long)discordGuildId);

        if (guild != null)
        {
            db.Guilds.Remove(guild);
            await db.SaveChangesAsync();
            await RemoveGuildFromCache(discordGuildId);
        }
    }
    
    public async Task<Maybe<Guild>> AddGuildAsync(ulong discordGuildId)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var guild = await db.Guilds.AsQueryable().FirstOrDefaultAsync(f => f.GuildId == (long)discordGuildId);

        if (guild == null)
        {
            guild = new Guild
            {
                GuildId = (long)discordGuildId,
                Prefix = "_"
                // TODO Prefix = this._botSettings.DefaultPrefix
            };
            await db.Guilds.AddAsync(guild);
            await db.SaveChangesAsync();
            await AddGuildToCache(guild);
        }

        return guild;
    }
    
    private Task AddGuildToCache(Guild guild)
    {
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(5));
        _cache.Set(CacheKeyForGuild((ulong)guild.GuildId), guild, cacheEntryOptions);
        return Task.CompletedTask;
    }
    
    public async Task<Maybe<Guild>> UpdateGuildPrefixAsync(ulong discordGuildId, string prefix)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
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
        _cache.Remove(CacheKeyForGuild(discordGuildId));
        return Task.CompletedTask;
    }

    private static string CacheKeyForGuild(ulong discordGuildId)
    {
        return $"guild-{discordGuildId}";
    }
    // TODO is there a more effective way of deleting something from the database? like not having to look first then delete
    public async Task DeleteGuildMember(ulong guildId, ulong discordUserId)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var guildMember = await db.GuildMembers
            .AsQueryable()
            .FirstOrDefaultAsync(f => f.GuildId == (long)guildId && f.UserId == (long)discordUserId);

        if (guildMember != null)
        {
            db.GuildMembers.Remove(guildMember);
            await db.SaveChangesAsync();
        }
    }

    public async Task<List<Guild>> FindGuildsForUser(ulong discordUserId)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var listOfGuildsForUser = await db.GuildMembers
            .AsQueryable()
            .Where(f => f.UserId == (long)discordUserId)
            .Select(s => s.Guild)
            .ToListAsync();
        return listOfGuildsForUser;
    }
}