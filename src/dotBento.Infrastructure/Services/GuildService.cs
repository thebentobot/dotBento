using CSharpFunctionalExtensions;
using Discord;
using Discord.WebSocket;
using dotBento.Domain;
using dotBento.EntityFramework.Context;
using dotBento.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dotBento.Infrastructure.Services;

public class GuildService(IDbContextFactory<BotDbContext> contextFactory, IMemoryCache cache)
{
    public async Task<Maybe<Guild>> GetGuildAsync(ulong discordGuildId)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var guild = await db.Guilds
            .AsQueryable()
            .FirstOrDefaultAsync(f => f.GuildId == (long)discordGuildId);
        
        return guild.AsMaybe();
    }

    public async Task<Maybe<Dictionary<long, GuildMember>>> GetGuildUsers(
        ulong discordGuildId)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var guildMembers = await db.GuildMembers
            .AsQueryable()
            .Where(f => f.GuildId == (long)discordGuildId)
            .ToDictionaryAsync(f => f.UserId);

        return guildMembers.AsMaybe();
    }
    
    public async Task<Maybe<GuildMember>> GetGuildMemberAsync(ulong discordGuildId,
        ulong discordUserId)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var guildMember = await db.GuildMembers
            .AsQueryable()
            .FirstOrDefaultAsync(f => f.GuildId == (long)discordGuildId && f.UserId == (long)discordUserId);

        return guildMember.AsMaybe();
    }

    public async Task RemoveGuildAsync(ulong discordGuildId)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var guild = await db.Guilds.AsQueryable().FirstOrDefaultAsync(f => f.GuildId == (long)discordGuildId);

        if (guild != null)
        {
            db.Guilds.Remove(guild);
            await db.SaveChangesAsync();
            await RemoveGuildFromCache(discordGuildId);
        }
    }
    
    public async Task AddGuildAsync(SocketGuild guild)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var databaseGuild = await db.Guilds.AsQueryable().FirstOrDefaultAsync(f => f.GuildId == (long)guild.Id);

        if (databaseGuild == null)
        {
            databaseGuild = new Guild
            {
                GuildId = (long)guild.Id,
                Prefix = Constants.StartPrefix,
                GuildName = guild.Name,
                MemberCount = guild.MemberCount,
            };
            await db.Guilds.AddAsync(databaseGuild);
            await db.SaveChangesAsync();
            await AddGuildToCache(databaseGuild);
        }
    }

    public async Task AddGuildMemberAsync(SocketGuildUser guildUser)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var guildMember = await db.GuildMembers
            .AsQueryable()
            .FirstOrDefaultAsync(f => f.GuildId == (long)guildUser.Guild.Id && f.UserId == (long)guildUser.Id);

        if (guildMember == null)
        {
            guildMember = new GuildMember
            {
                GuildId = (long)guildUser.Guild.Id,
                UserId = (long)guildUser.Id,
                AvatarUrl = guildUser.GetGuildAvatarUrl(ImageFormat.Auto, 512),
                Xp = 0,
                Level = 1
            };
            await db.GuildMembers.AddAsync(guildMember);
            await db.SaveChangesAsync();
            await AddGuildMemberToCache(guildMember);
        }
    }

    private Task AddGuildMemberToCache(GuildMember guildMember)
    {
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(5));
        cache.Set(CacheKeyForGuildMember((ulong)guildMember.GuildId, (ulong)guildMember.UserId), guildMember, cacheEntryOptions);
        return Task.CompletedTask;
    }

    private static string CacheKeyForGuildMember(ulong guildMemberGuildId, ulong guildMemberUserId)
    {
        return $"guild-member-{guildMemberGuildId}-{guildMemberUserId}";
    }
    
    private Task RemoveGuildMemberFromCache(ulong guildMemberGuildId, ulong guildMemberUserId)
    {
        cache.Remove(CacheKeyForGuildMember(guildMemberGuildId, guildMemberUserId));
        return Task.CompletedTask;
    }

    private Task AddGuildToCache(Guild guild)
    {
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(5));
        cache.Set(CacheKeyForGuild((ulong)guild.GuildId), guild, cacheEntryOptions);
        return Task.CompletedTask;
    }
    
    public async Task<Maybe<Guild>> UpdateGuildPrefixAsync(ulong discordGuildId, string prefix)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var guild = await db.Guilds.AsQueryable().FirstOrDefaultAsync(f => f.GuildId == (long)discordGuildId);

        if (guild == null) return Maybe<Guild>.None;
        guild.Prefix = prefix;
        await db.SaveChangesAsync();
        await AddGuildToCache(guild);

        return guild.AsMaybe();
    }
    
    private Task RemoveGuildFromCache(ulong discordGuildId)
    {
        cache.Remove(CacheKeyForGuild(discordGuildId));
        return Task.CompletedTask;
    }

    private static string CacheKeyForGuild(ulong discordGuildId)
    {
        return $"guild-{discordGuildId}";
    }
    
    public async Task DeleteGuildMember(ulong guildId, ulong discordUserId)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
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
        await using var db = await contextFactory.CreateDbContextAsync();
        var listOfGuildsForUser = await db.GuildMembers
            .AsQueryable()
            .Where(f => f.UserId == (long)discordUserId)
            .Select(s => s.Guild)
            .ToListAsync();
        return listOfGuildsForUser;
    }
    
    public async Task UpdateGuildMemberAvatarAsync(SocketGuildUser guildMember)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var user = await db.GuildMembers
            .AsQueryable()
            .FirstOrDefaultAsync(f => f.UserId == (long)guildMember.Id && f.GuildId == (long)guildMember.Guild.Id);

        if (user != null)
        {
            user.AvatarUrl = guildMember.GetGuildAvatarUrl(ImageFormat.Auto, 512);
            await db.SaveChangesAsync();
            await RemoveGuildMemberFromCache((ulong)user.GuildId, (ulong)user.UserId);
            await AddGuildMemberToCache(user);
        }
    }

    public async Task<int> GetTotalGuildCountAsync()
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.Guilds
            .AsQueryable()
            .CountAsync();
    }
    
    public async Task<Maybe<int>> GetGuildMemberRankAsync(long discordUserId, long discordGuildId)
    {
        await using var db = await contextFactory.CreateDbContextAsync();

        var guildMember = await db.GuildMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.GuildId == discordGuildId && u.UserId == discordUserId);

        if (guildMember == null)
        {
            return Maybe<int>.None;
        }

        var rank = await db.GuildMembers
            .AsNoTracking()
            .Where(u => u.GuildId == discordGuildId && 
                        (u.Level > guildMember.Level || 
                         (u.Level == guildMember.Level && u.Xp > guildMember.Xp)))
            .CountAsync();

        return rank + 1;
    }
}