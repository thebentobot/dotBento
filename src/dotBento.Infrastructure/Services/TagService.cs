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
    private readonly MemoryCacheEntryOptions _cacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
        SlidingExpiration = TimeSpan.FromMinutes(2)
    };
    private readonly Dictionary<long, List<string>> _cacheKeysByGuild = new();

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
        InvalidateTagsCache(guildId);

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
        InvalidateTagsCache(guildId);
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
        InvalidateTagsCache(guildId);
    }

    public async Task<Maybe<Tag>> FindTagAsync(long guildId, string name)
    {
        var cacheKey = GetTagCacheKey(guildId, name);
        if (cache.TryGetValue(cacheKey, out Maybe<Tag> tag)) return tag;
        await using var context = await contextFactory.CreateDbContextAsync();
        tag = (await context.Tags.FirstOrDefaultAsync(x => x.GuildId == guildId && x.Command == name))?.AsMaybe() ?? Maybe<Tag>.None;
            
        cache.Set(cacheKey, tag, _cacheOptions);
        AddCacheKey(guildId, cacheKey);
        return tag;
    }

    public async Task<Result<List<Tag>>> FindTagsAsync(long guildId, bool top, Maybe<long> authorId)
    {
        var cacheKey = GetTagsCacheKey(guildId);
        if (cache.TryGetValue(cacheKey, out Result<List<Tag>> tags))
            return top
                ? tags.Value.OrderByDescending(x => x.Count).ToList()
                : tags.Value;
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            var query = context.Tags.Where(x => x.GuildId == guildId);
            if (authorId.HasValue)
            {
                query = query.Where(x => x.UserId == authorId.Value);
            }
            tags = await query.ToListAsync();
            cache.Set(cacheKey, tags, _cacheOptions);
            AddCacheKey(guildId, cacheKey);
        }

        return top
            ? tags.Value.OrderByDescending(x => x.Count).ToList()
            : tags.Value;
    }

    public async Task<Maybe<Tag>> GetRandomTagAsync(long userId, long guildId)
    {
        var cacheKey = GetTagsCacheKey(guildId);
        if (cache.TryGetValue(cacheKey, out Result<List<Tag>> tags))
        {
            var random = new Random();
            var tag = tags.Value[random.Next(tags.Value.Count)];
            return tag.AsMaybe();
        }
        await using var context = await contextFactory.CreateDbContextAsync();
        var query = context.Tags.Where(x => x.GuildId == guildId);
        tags = await query.ToListAsync();
        cache.Set(cacheKey, tags, _cacheOptions);
        AddCacheKey(guildId, cacheKey);
        return tags.Value.Count != 0
            ? tags.Value[new Random().Next(tags.Value.Count)].AsMaybe()
            : Maybe<Tag>.None;
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
        //InvalidateTagCache(guildId, newName);
        InvalidateTagsCache(guildId);
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

        InvalidateTagsCache(tag.GuildId);
    }

    // TODO: use cache for search
    public async Task<List<Tag>> SearchTagsByCommandAsync(long guildId, string query)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Tags
            .Where(x => x.GuildId == guildId && EF.Functions.ILike(x.Command, $"%{query}%"))
            .ToListAsync();
    }

    // TODO: use cache for search
    public async Task<List<Tag>> SearchTagsByContentAsync(long guildId, string query)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Tags
            .Where(x => x.GuildId == guildId && EF.Functions.ILike(x.Content, $"%{query}%"))
            .ToListAsync();
    }

    private void InvalidateTagCache(long guildId, string name)
    {
        var cacheKey = GetTagCacheKey(guildId, name);
        cache.Remove(cacheKey);
        RemoveCacheKey(guildId, cacheKey);
    }

    private void InvalidateTagsCache(long guildId)
    {
        if (!_cacheKeysByGuild.TryGetValue(guildId, out var cacheKeys)) return;
        foreach (var cacheKey in cacheKeys)
        {
            cache.Remove(cacheKey);
        }
        _cacheKeysByGuild.Remove(guildId);
    }

    private string GetTagCacheKey(long guildId, string name) => $"Tag_{guildId}_{name}";
    
    private string GetTagsCacheKey(long guildId) => $"Tags_{guildId}";
    
    private void AddCacheKey(long guildId, string cacheKey)
    {
        if (!_cacheKeysByGuild.TryGetValue(guildId, out var value))
        {
            value = ([]);
            _cacheKeysByGuild[guildId] = value;
        }

        value.Add(cacheKey);
    }

    private void RemoveCacheKey(long guildId, string cacheKey)
    {
        if (!_cacheKeysByGuild.TryGetValue(guildId, out var cacheKeys)) return;
        cacheKeys.Remove(cacheKey);
        if (cacheKeys.Count == 0)
        {
            _cacheKeysByGuild.Remove(guildId);
        }
    }
}