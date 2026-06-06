using System.Diagnostics.CodeAnalysis;
using CSharpFunctionalExtensions;
using Discord;
using Discord.WebSocket;
using dotBento.Domain;
using dotBento.EntityFramework.Context;
using dotBento.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dotBento.Infrastructure.Services;

public sealed class GuildService(IDbContextFactory<BotDbContext> contextFactory, IMemoryCache cache)
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

    [ExcludeFromCodeCoverage(Justification = "Thin adapter over Discord.NET SocketGuild; internal overload covers the behavior.")]
    public async Task AddGuildAsync(SocketGuild guild)
    {
        await AddGuildAsync(guild.Id, guild.Name, guild.MemberCount);
    }

    internal async Task AddGuildAsync(ulong guildId, string name, int memberCount)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var databaseGuild = await db.Guilds.AsQueryable().FirstOrDefaultAsync(f => f.GuildId == (long)guildId);

        if (databaseGuild == null)
        {
            databaseGuild = new Guild
            {
                GuildId = (long)guildId,
                Prefix = Constants.StartPrefix,
                GuildName = name,
                MemberCount = memberCount,
            };
            await db.Guilds.AddAsync(databaseGuild);
            await db.SaveChangesAsync();
            await AddGuildToCache(databaseGuild);
        }
    }

    [ExcludeFromCodeCoverage(Justification = "Thin adapter over Discord.NET SocketGuildUser; internal overload covers the behavior.")]
    public async Task AddGuildMemberAsync(SocketGuildUser guildUser)
    {
        var avatarUrl = guildUser.GetGuildAvatarUrl(ImageFormat.Auto, 512) ??
                        guildUser.GetDisplayAvatarUrl(ImageFormat.Auto, 512);
        await AddGuildMemberAsync(guildUser.Guild.Id, guildUser.Id, avatarUrl);
    }

    internal async Task AddGuildMemberAsync(ulong guildId, ulong userId, string? avatarUrl)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var guildMember = await db.GuildMembers
            .AsQueryable()
            .FirstOrDefaultAsync(f => f.GuildId == (long)guildId && f.UserId == (long)userId);

        if (guildMember == null)
        {
            guildMember = new GuildMember
            {
                GuildId = (long)guildId,
                UserId = (long)userId,
                AvatarUrl = avatarUrl,
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
        cache.Set(CacheKeyForGuildMember((ulong)guildMember.GuildId, (ulong)guildMember.UserId), guildMember,
            cacheEntryOptions);
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

    [ExcludeFromCodeCoverage(Justification = "Thin adapter over Discord.NET SocketGuildUser; internal overload covers the behavior.")]
    public async Task UpdateGuildMemberAvatarAsync(SocketGuildUser guildMember)
    {
        var avatarUrl = guildMember.GetGuildAvatarUrl(ImageFormat.Auto, 512) ??
                        guildMember.GetDisplayAvatarUrl(ImageFormat.Auto, 512);
        await UpdateGuildMemberAvatarAsync(guildMember.Guild.Id, guildMember.Id, avatarUrl);
    }

    internal async Task UpdateGuildMemberAvatarAsync(ulong guildId, ulong userId, string? avatarUrl)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var user = await db.GuildMembers
            .AsQueryable()
            .FirstOrDefaultAsync(f => f.UserId == (long)userId && f.GuildId == (long)guildId);

        if (user != null)
        {
            user.AvatarUrl = avatarUrl;
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

    public async Task UpdateGuildMemberCountAsync(ulong discordGuildId, int memberCount)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var guild = await db.Guilds.AsQueryable().FirstOrDefaultAsync(f => f.GuildId == (long)discordGuildId);

        if (guild != null)
        {
            guild.MemberCount = memberCount;
            await db.SaveChangesAsync();
            await AddGuildToCache(guild);
        }
    }

    [ExcludeFromCodeCoverage(Justification = "Thin adapter over Discord.NET SocketGuildUser; internal overload covers the behavior.")]
    public async Task<Maybe<GuildMember>> GetOrCreateGuildMemberAsync(ulong discordGuildId, ulong discordUserId,
        SocketGuildUser guildUser)
    {
        var avatarUrl = guildUser.GetGuildAvatarUrl(ImageFormat.Auto, 512) ??
                        guildUser.GetDisplayAvatarUrl(ImageFormat.Auto, 512);
        return await GetOrCreateGuildMemberAsync(discordGuildId, discordUserId, avatarUrl);
    }

    internal async Task<Maybe<GuildMember>> GetOrCreateGuildMemberAsync(
        ulong discordGuildId,
        ulong discordUserId,
        string? avatarUrl)
    {
        var cachedKey = CacheKeyForGuildMember(discordGuildId, discordUserId);
        if (cache.TryGetValue(cachedKey, out GuildMember? cachedGuildMember))
        {
            return cachedGuildMember.AsMaybe();
        }

        await using var db = await contextFactory.CreateDbContextAsync();
        var guildMember = await db.GuildMembers
            .AsQueryable()
            .FirstOrDefaultAsync(member =>
                member.GuildId == (long)discordGuildId && member.UserId == (long)discordUserId);

        if (guildMember != null)
        {
            await AddGuildMemberToCache(guildMember);
            return guildMember.AsMaybe();
        }

        guildMember = new GuildMember
        {
            GuildId = (long)discordGuildId,
            UserId = (long)discordUserId,
            AvatarUrl = avatarUrl,
            Xp = 0,
            Level = 1
        };
        await db.GuildMembers.AddAsync(guildMember);
        await db.SaveChangesAsync();

        await AddGuildMemberToCache(guildMember);

        return guildMember.AsMaybe();
    }

    /// <summary>
    /// Gets a batch of guilds from the database for background processing.
    /// </summary>
    public async Task<List<Guild>> GetGuildBatchAsync(int batchSize, int skip)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.Guilds
            .AsNoTracking()
            .OrderBy(g => g.GuildId)
            .Skip(skip)
            .Take(batchSize)
            .ToListAsync();
    }

    /// <summary>
    /// Gets a batch of guild members from the database for background processing.
    /// </summary>
    public async Task<List<GuildMember>> GetGuildMemberBatchAsync(int batchSize, int skip)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.GuildMembers
            .AsNoTracking()
            .OrderBy(gm => gm.GuildMemberId)
            .Skip(skip)
            .Take(batchSize)
            .ToListAsync();
    }

    /// <summary>
    /// Updates guild information (name, icon) from Discord.
    /// </summary>
    public async Task<bool> SyncGuildFromDiscordAsync(Guild dbGuild, IGuild discordGuild)
    {
        var hasChanges = false;
        await using var db = await contextFactory.CreateDbContextAsync();
        var guild = await db.Guilds
            .FirstOrDefaultAsync(g => g.GuildId == dbGuild.GuildId);

        if (guild == null) return false;

        if (guild.GuildName != discordGuild.Name)
        {
            guild.GuildName = discordGuild.Name;
            hasChanges = true;
        }

        var newIconUrl = discordGuild.IconUrl;
        if (guild.Icon != newIconUrl)
        {
            guild.Icon = newIconUrl;
            hasChanges = true;
        }

        if (hasChanges)
        {
            await db.SaveChangesAsync();
            await RemoveGuildFromCache((ulong)guild.GuildId);
        }

        return hasChanges;
    }

    /// <summary>
    /// Updates guild member avatar from Discord.
    /// </summary>
    public async Task<bool> SyncGuildMemberFromDiscordAsync(GuildMember dbGuildMember, IGuildUser discordGuildUser)
    {
        var hasChanges = false;
        await using var db = await contextFactory.CreateDbContextAsync();
        var guildMember = await db.GuildMembers
            .FirstOrDefaultAsync(gm => gm.GuildMemberId == dbGuildMember.GuildMemberId);

        if (guildMember == null) return false;

        var newAvatarUrl = discordGuildUser.GetGuildAvatarUrl(ImageFormat.Auto, 512)
            ?? discordGuildUser.GetDisplayAvatarUrl(ImageFormat.Auto, 512);
        if (guildMember.AvatarUrl != newAvatarUrl)
        {
            guildMember.AvatarUrl = newAvatarUrl;
            hasChanges = true;
        }

        if (hasChanges)
        {
            await db.SaveChangesAsync();
            await RemoveGuildMemberFromCache((ulong)guildMember.GuildId, (ulong)guildMember.UserId);
        }

        return hasChanges;
    }

    /// <summary>
    /// Deletes guild members by their IDs in bulk.
    /// </summary>
    public async Task DeleteGuildMembersBulkAsync(List<long> guildMemberIds)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var guildMembers = await db.GuildMembers
            .Where(gm => guildMemberIds.Contains(gm.GuildMemberId))
            .ToListAsync();

        if (guildMembers.Count > 0)
        {
            db.GuildMembers.RemoveRange(guildMembers);
            await db.SaveChangesAsync();

            foreach (var gm in guildMembers)
            {
                await RemoveGuildMemberFromCache((ulong)gm.GuildId, (ulong)gm.UserId);
            }
        }
    }
}
