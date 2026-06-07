using CSharpFunctionalExtensions;
using dotBento.Domain.Entities;
using dotBento.Domain.Extensions;
using dotBento.Infrastructure.Extensions;
using dotBento.Infrastructure.Services;

namespace dotBento.Infrastructure.Commands;

public sealed class ReminderCommands(ReminderService reminderService)
{
    public async Task<Result> CreateReminderAsync(long userId, string content, DateTimeOffset date)
    {
        var existsCheck = await reminderService.GetReminderAsync(userId, content, date);
        if (existsCheck.HasValue)
        {
            return Result.Failure("Reminder already exists.");
        }
        if (date < DateTimeOffset.UtcNow)
        {
            return Result.Failure("Reminder date cannot be in the past.");
        }
        
        var sanitizedContent = SanitizeTagContent(content);
        if (string.IsNullOrWhiteSpace(sanitizedContent))
        {
            return Result.Failure("Reminder content cannot be empty.");
        }

        var reminder = await reminderService.CreateReminderAsync(userId, sanitizedContent, date);
        return Result.Success(reminder);
    }
    
    public async Task<Result> DeleteReminderAsync(long userId, int reminderId)
    {
        var existsCheck = await reminderService.GetReminderAsync(userId, reminderId);
        if (!existsCheck.HasValue)
        {
            return Result.Failure("Reminder not found.");
        }
        await reminderService.DeleteReminderAsync(userId, reminderId);
        return Result.Success();
    }
    
    public async Task<Result> UpdateReminderAsync(long userId, int reminderId, string? content, DateTimeOffset? date)
    {
        var existsCheck = await reminderService.GetReminderAsync(userId, reminderId);
        if (!existsCheck.HasValue)
        {
            return Result.Failure("Reminder not found.");
        }
        if (content == null && date == null)
        {
            return Result.Failure("Reminder content or date must be provided.");
        }
        var dateCheck = date ?? existsCheck.Value.Date;
        if (dateCheck < DateTimeOffset.UtcNow)
        {
            return Result.Failure("Reminder date cannot be in the past.");
        }
        var contentCheck = SanitizeTagContent(content ?? existsCheck.Value.Reminder1);
        if (string.IsNullOrWhiteSpace(contentCheck))
        {
            return Result.Failure("Reminder content cannot be empty.");
        }
        await reminderService.UpdateReminderAsync(userId, reminderId, contentCheck, dateCheck);
        return Result.Success();
    }
    
    public async Task<Result<Reminder>> GetReminderAsync(long userId, int reminderId)
    {
        var reminder = await reminderService.GetReminderAsync(userId, reminderId);
        return reminder.HasValue ? Result.Success(reminder.Value.Map()) : Result.Failure<Reminder>("Reminder not found.");
    }
    
    public async Task<Result<List<Reminder>>> GetRemindersAsync(long userId)
    {
        var reminders = await reminderService.GetRemindersAsync(userId);
        return reminders.Count > 0 ? Result.Success(reminders.Select(x => x.Map()).OrderBy(x => x.Date).ToList()) : Result.Failure<List<Reminder>>("No reminders found.");
    }
    
    public async Task<Result<List<Reminder>>> GetAllRecentRemindersAsync()
    {
        var reminders = await reminderService.GetAllRecentRemindersAsync();
        return reminders.Count > 0 ? Result.Success(reminders.Select(x => x.Map()).ToList()) : Result.Failure<List<Reminder>>("No reminders found.");
    }
    
    private static string SanitizeTagContent(string content) =>
        content.Sanitize().TrimToMaxLength(2000);
}