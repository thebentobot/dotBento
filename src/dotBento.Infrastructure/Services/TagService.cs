using CSharpFunctionalExtensions;
using dotBento.EntityFramework.Context;
using dotBento.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dotBento.Infrastructure.Services;

public class TagService(
    IMemoryCache cache,
    IDbContextFactory<BotDbContext> contextFactory)
{
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
    }
    
    public async Task<Maybe<Tag>> FindTagAsync(long userId, long guildId, string name)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var tag = await context.Tags.FirstOrDefaultAsync(x =>
            x.UserId == userId && x.GuildId == guildId && x.Command == name);
        return tag?.AsMaybe() ?? Maybe<Tag>.None;
    }
    
    public async Task<IReadOnlyList<Tag>> FindTagsAsync(long userId, long guildId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Tags.Where(x => x.UserId == userId && x.GuildId == guildId).ToListAsync();
    }
    
    public async Task<Maybe<Tag>> GetRandomTagAsync(long userId, long guildId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var tags = await context.Tags.Where(x => x.UserId == userId && x.GuildId == guildId).ToListAsync();
        return tags.Count == 0 ? Maybe<Tag>.None : tags[new Random().Next(tags.Count)].AsMaybe();
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
    }
    
    public async Task<IReadOnlyList<Tag>> SearchTagsAsync(long guildId, string query)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Tags
            .Where(x => x.GuildId == guildId && EF.Functions.ILike(x.Command, $"%{query}%"))
            .ToListAsync();
    }
    
    public async Task<IReadOnlyList<Tag>> GetTopTagsAsync(long guildId, int count)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Tags
            .Where(x => x.GuildId == guildId)
            .OrderByDescending(x => x.Count)
            .Take(count)
            .ToListAsync();
    }
}