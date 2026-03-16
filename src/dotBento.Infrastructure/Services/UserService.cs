using CSharpFunctionalExtensions;
using NetCord.Gateway;
using dotBento.EntityFramework.Context;
using dotBento.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using DiscordGuild = NetCord.Gateway.Guild;

namespace dotBento.Infrastructure.Services;

public sealed class UserService(IMemoryCache cache,
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

        cache.TryGetValue(discordUserIdCacheKey, out User? user);

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

    public static Task<string> GetNameAsync(DiscordGuild? guild, NetCord.User user)
    {
        if (guild == null)
        {
            return Task.FromResult(user.GlobalName ?? user.Username);
        }

        if (guild.Users.TryGetValue(user.Id, out var guildMember))
        {
            return Task.FromResult(guildMember.Nickname ?? guildMember.GlobalName ?? guildMember.Username);
        }

        return Task.FromResult(user.GlobalName ?? user.Username);
    }

    public async Task<int> GetTotalDatabaseUserCountAsync()
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.Users
            .AsQueryable()
            .CountAsync();
    }

    public async Task<Maybe<int>> GetTotalDiscordUserCountAsync()
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var memberCount = db.Guilds
            .AsQueryable()
            .SumAsync(s => s.MemberCount).Result;

        return memberCount.AsMaybe();
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

    public async Task CreateOrAddUserToCache(NetCord.User discordUser)
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
                Discriminator = discordUser.Discriminator.ToString(),
                AvatarUrl = GetAvatarUrl(discordUser),
                Level = 1,
                Xp = 0
            };

            db.Users.Add(databaseUser);
            await db.SaveChangesAsync();
        }

        await AddUserToCache(databaseUser);
    }

    public async Task CreateOrAddUserToCache(NetCord.GuildUser discordUser)
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
                Discriminator = discordUser.Discriminator.ToString(),
                AvatarUrl = GetAvatarUrl(discordUser),
                Level = 1,
                Xp = 0
            };

            db.Users.Add(databaseUser);
            await db.SaveChangesAsync();
        }

        await AddUserToCache(databaseUser);
    }

    public async Task UpdateUserAvatarAsync(NetCord.User newUser)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var user = await db.Users
            .AsQueryable()
            .FirstOrDefaultAsync(f => f.UserId == (long)newUser.Id);

        if (user != null)
        {
            user.AvatarUrl = GetAvatarUrl(newUser);
            await db.SaveChangesAsync();
            RemoveUserFromCache(user);
            await AddUserToCache(user);
        }
    }

    public async Task UpdateUserUsernameAsync(NetCord.User newUser)
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

    // TODO change to long instead of casting it at this level as we usually cast at an upper level
    public async Task<Maybe<User>> GetUserAsync(ulong userId)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.UserId == (long)userId);
        var maybeUser = user.AsMaybe();

        if (maybeUser.HasValue)
        {
            await AddUserToCache(maybeUser.Value);
        }

        return maybeUser;
    }

    public async Task AddExperienceAsync(ulong userId, ulong guildId, Maybe<Patreon> patreonUser)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var user = await db.Users
            .AsQueryable()
            .FirstOrDefaultAsync(f => f.UserId == (long)userId);
        var guildMember = await db.GuildMembers
            .AsQueryable()
            .FirstOrDefaultAsync(f => f.UserId == (long)userId && f.GuildId == (long)guildId);

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

    public async Task<Maybe<int>> GetUserRankAsync(long userId)
    {
        await using var db = await contextFactory.CreateDbContextAsync();

        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            return Maybe<int>.None;
        }

        var rank = await db.Users
            .AsNoTracking()
            .Where(u => u.Level > user.Level || (u.Level == user.Level && u.Xp > user.Xp))
            .CountAsync();

        return rank + 1;
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
    /*
     TODO: Add this to the command handler
    public async Task AddUserSlashCommandInteraction(SocketInteractionContext context, string commandName)
    {
        var user = await GetUserAsync(context.User.Id);

        await Task.Delay(10000);

        try
        {
            if (user.HasValue)
            {
                var commandResponse = CommandResponse.Ok;
                if (PublicProperties.UsedCommandsResponses.TryGetValue(context.Interaction.Id, out var fetchedResponse))
                {
                    commandResponse = fetchedResponse;
                }

                string errorReference = null;
                if (PublicProperties.UsedCommandsErrorReferences.TryGetValue(context.Interaction.Id, out var fetchedErrorId))
                {
                    errorReference = fetchedErrorId;
                }

                var options = new Dictionary<string, string>();
                if (context.Interaction is SocketSlashCommand command)
                {
                    foreach (var option in command.Data.Options)
                    {
                        options.Add(option.Name, option.Value?.ToString());
                    }
                }
                var interaction = new UserInteraction
                {
                    Timestamp = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                    CommandName = commandName,
                    CommandOptions = options.Any() ? options : null,
                    UserId = user.UserId,
                    DiscordGuildId = context.Guild?.Id,
                    DiscordChannelId = context.Channel?.Id,
                    DiscordId = context.Interaction.Id,
                    Response = commandResponse,
                    Type = UserInteractionType.SlashCommand,
                    ErrorReferenceId = errorReference
                };

                await using var db = await this._contextFactory.CreateDbContextAsync();
                await db.UserInteractions.AddAsync(interaction);
                await db.SaveChangesAsync();

            }
        }
        catch (Exception e)
        {
            Log.Error(e, "AddUserSlashCommandInteraction: Error while adding user interaction");
        }
    }
    */

    /// <summary>
    /// Gets a batch of users from the database for background processing.
    /// </summary>
    public async Task<List<User>> GetUserBatchAsync(int batchSize, int skip)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.Users
            .AsNoTracking()
            .OrderBy(u => u.UserId)
            .Skip(skip)
            .Take(batchSize)
            .ToListAsync();
    }

    /// <summary>
    /// Updates user information (username, discriminator, avatar) from Discord.
    /// </summary>
    public async Task<bool> SyncUserFromDiscordAsync(User dbUser, NetCord.User discordUser)
    {
        var hasChanges = false;
        await using var db = await contextFactory.CreateDbContextAsync();
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.UserId == dbUser.UserId);

        if (user == null) return false;

        var newAvatarUrl = GetAvatarUrl(discordUser);
        if (user.AvatarUrl != newAvatarUrl)
        {
            user.AvatarUrl = newAvatarUrl;
            hasChanges = true;
        }

        if (user.Username != discordUser.Username)
        {
            user.Username = discordUser.Username;
            hasChanges = true;
        }

        if (user.Discriminator != discordUser.Discriminator.ToString())
        {
            user.Discriminator = discordUser.Discriminator.ToString();
            hasChanges = true;
        }

        if (hasChanges)
        {
            await db.SaveChangesAsync();
            RemoveUserFromCache(user);
        }

        return hasChanges;
    }

    public async Task<List<long>> GetUsersWithoutGuilds()
    {
        await using var db = await contextFactory.CreateDbContextAsync();

        return await db.Users
            .Where(u => !u.GuildMembers.Any())
            .Select(u => u.UserId)
            .ToListAsync();
    }

    private static string? GetAvatarUrl(NetCord.User user) =>
        user.AvatarHash != null
            ? $"https://cdn.discordapp.com/avatars/{user.Id}/{user.AvatarHash}.{(user.AvatarHash.StartsWith("a_") ? "gif" : "webp")}?size=512"
            : null;
}
