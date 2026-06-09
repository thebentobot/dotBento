using dotBento.EntityFramework.Entities;
using dotBento.Infrastructure.Commands;
using dotBento.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;

namespace dotBento.Infrastructure.Tests.Commands;

public sealed class ReminderCommandsTests
{
    [Fact]
    public async Task CreateReminderAsync_ValidatesAndCreatesReminder()
    {
        var command = CreateCommand(out var factory);
        var date = DateTimeOffset.UtcNow.AddHours(1);

        var created = await command.CreateReminderAsync(100, " hello #tag ", date);
        var duplicate = await command.CreateReminderAsync(100, " hello #tag ", date);
        var pastDate = await command.CreateReminderAsync(100, "past", DateTimeOffset.UtcNow.AddHours(-1));
        var empty = await command.CreateReminderAsync(100, "   ", date.AddHours(1));

        Assert.True(created.IsSuccess);
        Assert.True(duplicate.IsFailure);
        Assert.Equal("Reminder already exists.", duplicate.Error);
        Assert.True(pastDate.IsFailure);
        Assert.True(empty.IsFailure);

        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var reminder = Assert.Single(db.Reminders);
        Assert.Equal(@" hello \#tag ", reminder.Reminder1);
    }

    [Fact]
    public async Task DeleteUpdateAndGetReminderAsync_ValidateAndPersistChanges()
    {
        var command = CreateCommand(out var factory);
        var reminder = await SeedReminderAsync(factory, 100, "original", DateTime.UtcNow.AddHours(1));

        var missing = await command.GetReminderAsync(100, 999);
        var found = await command.GetReminderAsync(100, reminder.Id);
        var noChanges = await command.UpdateReminderAsync(100, reminder.Id, null, null);
        var pastDate = await command.UpdateReminderAsync(100, reminder.Id, null, DateTimeOffset.UtcNow.AddHours(-1));
        var emptyContent = await command.UpdateReminderAsync(100, reminder.Id, "   ", null);
        var updateMissing = await command.UpdateReminderAsync(100, 999, "updated", null);
        var updated = await command.UpdateReminderAsync(100, reminder.Id, "updated", DateTimeOffset.UtcNow.AddHours(2));
        var afterUpdate = await command.GetReminderAsync(100, reminder.Id);
        var deleteMissing = await command.DeleteReminderAsync(100, 999);
        var deleted = await command.DeleteReminderAsync(100, reminder.Id);
        var afterDelete = await command.GetReminderAsync(100, reminder.Id);

        Assert.True(missing.IsFailure);
        Assert.True(found.IsSuccess);
        Assert.True(noChanges.IsFailure);
        Assert.True(pastDate.IsFailure);
        Assert.True(emptyContent.IsFailure);
        Assert.True(updateMissing.IsFailure);
        Assert.True(updated.IsSuccess);
        Assert.Equal("updated", afterUpdate.Value.Content);
        Assert.True(deleteMissing.IsFailure);
        Assert.True(deleted.IsSuccess);
        Assert.True(afterDelete.IsFailure);
    }

    [Fact]
    public async Task GetRemindersAsync_ReturnsOrderedRemindersOrFailureWhenEmpty()
    {
        var command = CreateCommand(out var factory);
        var empty = await command.GetRemindersAsync(100);
        await SeedReminderAsync(factory, 100, "later", DateTime.UtcNow.AddHours(2));
        await SeedReminderAsync(factory, 100, "sooner", DateTime.UtcNow.AddHours(1));

        var result = await command.GetRemindersAsync(100);

        Assert.True(empty.IsFailure);
        Assert.True(result.IsSuccess);
        Assert.Equal(["sooner", "later"], result.Value.Select(reminder => reminder.Content));
    }

    [Fact]
    public async Task GetAllRecentRemindersAsync_ReturnsPastRemindersOrFailureWhenEmpty()
    {
        var command = CreateCommand(out var factory);
        var empty = await command.GetAllRecentRemindersAsync();
        await SeedReminderAsync(factory, 100, "past", DateTime.UtcNow.AddMinutes(-1));
        await SeedReminderAsync(factory, 100, "future", DateTime.UtcNow.AddMinutes(10));

        var result = await command.GetAllRecentRemindersAsync();

        Assert.True(empty.IsFailure);
        Assert.True(result.IsSuccess);
        var reminder = Assert.Single(result.Value);
        Assert.Equal("past", reminder.Content);
    }

    private static ReminderCommands CreateCommand(out InfrastructureTestDbFactory factory)
    {
        factory = new InfrastructureTestDbFactory();
        return new ReminderCommands(new ReminderService(new MemoryCache(new MemoryCacheOptions()), factory));
    }

    private static async Task<Reminder> SeedReminderAsync(
        InfrastructureTestDbFactory factory,
        long userId,
        string content,
        DateTime date)
    {
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var reminder = new Reminder
        {
            UserId = userId,
            Reminder1 = content,
            Date = date
        };
        db.Reminders.Add(reminder);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return reminder;
    }
}
