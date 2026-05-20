using CSharpFunctionalExtensions;
using dotBento.EntityFramework.Context;
using dotBento.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dotBento.Infrastructure.Services;

public sealed class TagService(
    IMemoryCache cache,
    IDbContextFactory<BotDbContext> contextFactory)
{
    private const int AutocompleteLimit = 25;

    private readonly MemoryCacheEntryOptions _cacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
        SlidingExpiration = TimeSpan.FromMinutes(2)
    };

    public async Task<Tag> CreateTagAsync(long userId, long guildId, string name, string content)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var tag = new Tag
        {
            UserId = userId,
            Command = name,
            Content = content,
            Date = DateTime.UtcNow,
            Count = 0,
            GuildId = guildId,
        };
        await context.Tags.AddAsync(tag);
        await context.SaveChangesAsync();

        // TODO: should we invalidate instead of adding to cache?
        InvalidateTagCache(guildId, name);

        return tag;
    }

    public async Task DeleteTagAsync(long userId, long guildId, string name)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var tag = await context.Tags.FirstOrDefaultAsync(x =>
            x.UserId == userId && x.GuildId == guildId && x.Command == name);
        if (tag == null)
        {
            return;
        }
        context.Tags.Remove(tag);
        await context.SaveChangesAsync();

        InvalidateTagCache(guildId, name);
    }

    public async Task UpdateTagAsync(long userId, long guildId, string name, string content)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var tag = await context.Tags.FirstOrDefaultAsync(x =>
            x.UserId == userId && x.GuildId == guildId && x.Command == name);
        if (tag == null)
        {
            return;
        }
        tag.Content = content;
        await context.SaveChangesAsync();

        InvalidateTagCache(guildId, name);
    }

    public async Task<Maybe<Tag>> FindTagAsync(long guildId, string name)
    {
        var cacheKey = GetTagCacheKey(guildId, name);
        if (cache.TryGetValue(cacheKey, out Maybe<Tag> tag)) return tag;
        await using var context = await contextFactory.CreateDbContextAsync();
        tag = (await context.Tags
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.GuildId == guildId && x.Command == name))?.AsMaybe() ?? Maybe<Tag>.None;
            
        cache.Set(cacheKey, tag, _cacheOptions);
        return tag;
    }

    public async Task<Result<List<Tag>>> FindTagsAsync(long guildId, bool top, Maybe<long> authorId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var query = context.Tags
            .AsNoTracking()
            .Where(x => x.GuildId == guildId);

        if (authorId.HasValue)
        {
            query = query.Where(x => x.UserId == authorId.Value);
        }

        query = top
            ? query.OrderByDescending(x => x.Count)
            : query.OrderBy(x => x.Command);

        var tags = await query.ToListAsync();
        return Result.Success(tags);
    }

    public async Task<Maybe<Tag>> GetRandomTagAsync(long userId, long guildId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var query = context.Tags
            .AsNoTracking()
            .Where(x => x.GuildId == guildId);
        var count = await query.CountAsync();
        if (count == 0)
        {
            return Maybe<Tag>.None;
        }

        var tag = await query
            .OrderBy(x => x.TagId)
            .Skip(Random.Shared.Next(count))
            .FirstOrDefaultAsync();
        return tag.AsMaybe();
    }

    public async Task RenameTagAsync(long userId, long guildId, string oldName, string newName)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var tag = await context.Tags.FirstOrDefaultAsync(x =>
            x.UserId == userId && x.GuildId == guildId && x.Command == oldName);
        if (tag == null)
        {
            return;
        }
        tag.Command = newName;
        await context.SaveChangesAsync();

        InvalidateTagCache(guildId, oldName);
        InvalidateTagCache(guildId, newName);
    }

    public async Task IncrementTagCountAsync(long tagId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var tag = await context.Tags.FirstOrDefaultAsync(x => x.TagId == tagId);
        if (tag == null)
        {
            return;
        }
        tag.Count++;
        await context.SaveChangesAsync();

        InvalidateTagCache(tag.GuildId, tag.Command);
    }

    public async Task<List<Tag>> SearchTagsByCommandAsync(long guildId, string query)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Tags
            .AsNoTracking()
            .Where(x => x.GuildId == guildId && EF.Functions.ILike(x.Command, $"%{query}%"))
            .ToListAsync();
    }

    public async Task<List<Tag>> SearchTagsByContentAsync(long guildId, string query)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Tags
            .AsNoTracking()
            .Where(x => x.GuildId == guildId && EF.Functions.ILike(x.Content, $"%{query}%"))
            .ToListAsync();
    }

    public async Task<List<string>> FindTagNamesForAutocompleteAsync(long guildId, Maybe<long> authorId, string? query)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var tags = context.Tags
            .AsNoTracking()
            .Where(x => x.GuildId == guildId);

        if (authorId.HasValue)
        {
            tags = tags.Where(x => x.UserId == authorId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            tags = tags.Where(x => x.Command.StartsWith(query));
        }

        return await tags
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Command)
            .Select(x => x.Command)
            .Take(AutocompleteLimit)
            .ToListAsync();
    }

    private void InvalidateTagCache(long guildId, string name)
    {
        var cacheKey = GetTagCacheKey(guildId, name);
        cache.Remove(cacheKey);
    }

    private string GetTagCacheKey(long guildId, string name) => $"Tag_{guildId}_{name}";
}
