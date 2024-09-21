using CSharpFunctionalExtensions;
using dotBento.EntityFramework.Context;
using dotBento.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dotBento.Infrastructure.Services;

public sealed class ReminderService(
    IMemoryCache cache,
    IDbContextFactory<BotDbContext> contextFactory)
{
    private readonly MemoryCacheEntryOptions _cacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
        SlidingExpiration = TimeSpan.FromMinutes(2)
    };
    
    public async Task<Reminder> CreateReminderAsync(long userId, string content, DateTimeOffset date)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var reminder = new Reminder
        {
            UserId = userId,
            Reminder1 = content,
            Date = date.UtcDateTime
        };
        await context.Reminders.AddAsync(reminder);
        await context.SaveChangesAsync();
        
        cache.Set($"reminder:{reminder.Id}", reminder, _cacheOptions);

        return reminder;
    }
    
    public async Task DeleteReminderAsync(long userId, int reminderId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var reminder = await context.Reminders.FirstOrDefaultAsync(x =>
            x.UserId == userId && x.Id == reminderId);
        if (reminder == null)
        {
            return;
        }
        context.Reminders.Remove(reminder);
        await context.SaveChangesAsync();
        
        cache.Remove($"reminder:{reminder.Id}");
    }
    
    public async Task UpdateReminderAsync(long userId, int reminderId, string content, DateTimeOffset date)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var reminder = await context.Reminders.FirstOrDefaultAsync(x =>
            x.UserId == userId && x.Id == reminderId);
        if (reminder == null)
        {
            return;
        }
        reminder.Reminder1 = content;
        reminder.Date = date.UtcDateTime;
        await context.SaveChangesAsync();
        cache.Remove($"reminder:{reminder.Id}");
        cache.Set($"reminder:{reminder.Id}", reminder, _cacheOptions);
    }
    
    public async Task<Maybe<Reminder>> GetReminderAsync(long userId, int reminderId)
    {
        if (cache.TryGetValue($"reminder:{reminderId}", out Maybe<Reminder> reminder))
        {
            return reminder;
        }
        
        await using var context = await contextFactory.CreateDbContextAsync();
        reminder = await context.Reminders.FirstOrDefaultAsync(x =>
            x.UserId == userId && x.Id == reminderId) ?? Maybe<Reminder>.None;
        
        cache.Set($"reminder:{reminderId}", reminder, _cacheOptions);
        
        return reminder;
    }
    
    public async Task<Maybe<Reminder>> GetReminderAsync(long userId, string content, DateTimeOffset date)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var reminder = await context.Reminders.FirstOrDefaultAsync(x =>
            x.UserId == userId && x.Reminder1 == content && x.Date == date.UtcDateTime)?? Maybe<Reminder>.None;
        if (!reminder.HasValue) return Maybe<Reminder>.None;
        cache.Set($"reminder:{reminder.Value.Id}", reminder, _cacheOptions);
        return reminder;
    }
    
    public async Task<List<Reminder>> GetRemindersAsync(long userId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var reminders = await context.Reminders.Where(x => x.UserId == userId).ToListAsync();
        foreach (var reminder in reminders)
        {
            cache.Set($"reminder:{reminder.Id}", reminder, _cacheOptions);
        }
        
        return reminders;
    }
    
    public async Task<List<Reminder>> GetAllRecentRemindersAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Reminders
            .Where(x => x.Date < DateTime.UtcNow)
            .ToListAsync();
    }
}