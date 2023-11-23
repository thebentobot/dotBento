using CSharpFunctionalExtensions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using dotBento.EntityFramework.Context;
using dotBento.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dotBento.Bot.Services;

public class UserService(IMemoryCache cache,
    IDbContextFactory<BotDbContext> contextFactory)
{
    public async Task<Maybe<User>> GetUserFromDatabaseAsync(ulong discordUserId)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var user = await db.Users
            .AsNoTracking()
            .FirstAsync(f => f.UserId == (long)discordUserId);

        return user.AsMaybe();
    }

    public Task<Maybe<User>> GetUserFromCache(ulong discordUserId)
    {
        var discordUserIdCacheKey = UserDiscordIdCacheKey((long)discordUserId);

        cache.TryGetValue(discordUserIdCacheKey, out User user);

        return Task.FromResult(user.AsMaybe());
    }

    private void RemoveUserFromCache(User user)
    {
        cache.Remove(UserDiscordIdCacheKey(user.UserId));
    }
    
    private Task AddUserToCache(User user)
    {
        var discordUserIdCacheKey = UserDiscordIdCacheKey(user.UserId);
        cache.Set(discordUserIdCacheKey, user, TimeSpan.FromMinutes(5));
        return Task.CompletedTask;
    }

    private static string UserDiscordIdCacheKey(long discordUserId)
    {
        return $"user-{discordUserId}";
    }
    
    public async Task<Dictionary<long, User>> GetMultipleUsers(HashSet<int> userIds)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.Users
            .AsNoTracking()
            .Where(w => userIds.Contains((int)w.UserId))
            .ToDictionaryAsync(d => d.UserId, d => d);
    }
    
    public async Task<List<User>> GetAllDiscordUserIds()
    {
        await using var db = await contextFactory.CreateDbContextAsync();
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
    
    public async Task<int> GetTotalDatabaseUserCountAsync()
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.Users
            .AsQueryable()
            .CountAsync();
    }

    public async Task<int> GetTotalDiscordUserCountAsync()
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.Guilds
            .AsQueryable()
            .SumAsync(s => (int)s.MemberCount);
    }

    public async Task DeleteUserAsync(ulong discordUserId)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
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

    public async Task AddUserAsync(SocketUser discordUser)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var databaseUser = await db.Users
            .AsQueryable()
            .FirstOrDefaultAsync(f => f.UserId == (long)discordUser.Id);

        if (databaseUser == null)
        {
            databaseUser = new User
            {
                UserId = (long)discordUser.Id,
                Username = discordUser.Username,
                Discriminator = discordUser.Discriminator,
                AvatarUrl = discordUser.GetAvatarUrl(ImageFormat.Auto, 512),
                Level = 1,
                Xp = 0
            };

            db.Users.Add(databaseUser);
            await db.SaveChangesAsync();
        }
        
        await AddUserToCache(databaseUser);
    }

    public async Task UpdateUserAvatarAsync(SocketUser newUser)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var user = await db.Users
            .AsQueryable()
            .FirstOrDefaultAsync(f => f.UserId == (long)newUser.Id);

        if (user != null)
        {
            user.AvatarUrl = newUser.GetAvatarUrl(ImageFormat.Auto, 512);
            await db.SaveChangesAsync();
            RemoveUserFromCache(user);
            await AddUserToCache(user);
        }
    }

    public async Task UpdateUserUsernameAsync(SocketUser newUser)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var user = await db.Users
            .AsQueryable()
            .FirstOrDefaultAsync(f => f.UserId == (long)newUser.Id);

        if (user != null)
        {
            user.Username = newUser.Username;
            await db.SaveChangesAsync();
            RemoveUserFromCache(user);
            await AddUserToCache(user);
        }
    }

    public async Task<Maybe<Patreon>> GetPatreonUserAsync(ulong userId)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var patreon = await db.Patreons
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.UserId == (long)userId);

        return patreon.AsMaybe();
    }

    public async Task AddExperienceAsync(SocketCommandContext context, Maybe<Patreon> patreonUser)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var user = await db.Users
            .AsQueryable()
            .FirstOrDefaultAsync(f => f.UserId == (long)context.User.Id);
        var guildMember = await db.GuildMembers
            .AsQueryable()
            .FirstOrDefaultAsync(f => f.UserId == (long)context.User.Id && f.GuildId == (long)context.Guild.Id);
        
        if (user == null || guildMember == null) return;

        var experiencePoints = 23;
        
        if (patreonUser.HasValue)
        {
            experiencePoints = GetExperiencePointsForPatreonUser(patreonUser.Value);
        }
        
        user.Xp += experiencePoints;
        guildMember.Xp += experiencePoints;
        
        var neededExperienceUser = GetNeededExperienceByLevel(user.Level);
        var neededExperienceGuildMember = GetNeededExperienceByLevel(guildMember.Level);
        
        if (user.Xp >= neededExperienceUser)
        {
            user.Level++;
            user.Xp = 0;
        }
        
        if (guildMember.Xp >= neededExperienceGuildMember)
        {
            guildMember.Level++;
            guildMember.Xp = 0;
        }
        
        await db.SaveChangesAsync();
    }
    
    private int GetNeededExperienceByLevel(int level)
    {
        return level * level * 100;
    }

    private int GetExperiencePointsForPatreonUser(Patreon patreonUser)
    {
        if (patreonUser.Follower)
        {
            return 46;
        }

        if (patreonUser.Enthusiast)
        {
            return 69;
        }

        if (patreonUser.Disciple)
        {
            return 92;
        }

        return 115;
    }
}