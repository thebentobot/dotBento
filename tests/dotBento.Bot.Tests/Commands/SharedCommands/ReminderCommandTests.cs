using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Enums;
using dotBento.EntityFramework.Entities;
using dotBento.Infrastructure.Commands;
using dotBento.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;

namespace dotBento.Bot.Tests.Commands.SharedCommands;

public sealed class ReminderCommandTests
{
    private static (ReminderCommand Command, TestDbFactory Factory) CreateCommand()
    {
        var factory = new TestDbFactory();
        var command = new ReminderCommand(new ReminderCommands(new ReminderService(new MemoryCache(new MemoryCacheOptions()), factory)));
        return (command, factory);
    }

    [Fact]
    public async Task CreateReminderAsync_ReturnsSuccessAndErrorResponses()
    {
        var (command, _) = CreateCommand();
        var date = DateTimeOffset.UtcNow.AddHours(1);

        var created = await command.CreateReminderAsync(10, "remember", date);
        var duplicate = await command.CreateReminderAsync(10, "remember", date);

        Assert.Equal("Reminder created successfully.", created.Embed.Build().Title);
        Assert.Equal("Error", duplicate.Embed.Build().Title);
    }

    [Fact]
    public async Task DeleteReminderAsync_ReturnsSuccessAndErrorResponses()
    {
        var (command, factory) = CreateCommand();
        var reminder = await SeedReminderAsync(factory, 10, "remember", DateTime.UtcNow.AddHours(1));

        var deleted = await command.DeleteReminderAsync(10, reminder.Id);
        var missing = await command.DeleteReminderAsync(10, reminder.Id);

        Assert.Equal("Reminder deleted successfully.", deleted.Embed.Build().Title);
        Assert.Equal("Error", missing.Embed.Build().Title);
    }

    [Fact]
    public async Task UpdateReminderAsync_FormatsOptionalChanges()
    {
        var (command, factory) = CreateCommand();
        var reminder = await SeedReminderAsync(factory, 10, "old", DateTime.UtcNow.AddHours(1));
        var newDate = DateTimeOffset.UtcNow.AddHours(2);

        var updated = await command.UpdateReminderAsync(10, reminder.Id, "new", newDate);
        var missing = await command.UpdateReminderAsync(10, 999, "new", null);
        var embed = updated.Embed.Build();

        Assert.Equal("Reminder updated successfully.\nRemember to have DMs enabled to receive reminders.", embed.Title);
        Assert.Contains("New content: `new`", embed.Description);
        Assert.Contains("New date:", embed.Description);
        Assert.Equal("Error", missing.Embed.Build().Title);
    }

    [Fact]
    public async Task GetReminderAsync_ReturnsReminderDetails()
    {
        var (command, factory) = CreateCommand();
        var reminder = await SeedReminderAsync(factory, 10, "remember", DateTime.UtcNow.AddHours(1));

        var response = await command.GetReminderAsync(10, reminder.Id);
        var missing = await command.GetReminderAsync(10, 999);

        Assert.Equal("Reminder", response.Embed.Build().Title);
        Assert.Contains("Content: `remember`", response.Embed.Build().Description);
        Assert.Equal("Error", missing.Embed.Build().Title);
    }

    [Fact]
    public async Task GetRemindersAsync_ReturnsPaginatorOrError()
    {
        var (command, factory) = CreateCommand();
        var empty = await command.GetRemindersAsync(10);
        await SeedReminderAsync(factory, 10, "remember", DateTime.UtcNow.AddHours(1));

        var response = await command.GetRemindersAsync(10);

        Assert.Equal("Error", empty.Embed.Build().Title);
        Assert.Equal(ResponseType.Paginator, response.ResponseType);
        Assert.NotNull(response.StaticPaginator);
    }

    private static async Task<Reminder> SeedReminderAsync(TestDbFactory factory, long userId, string content, DateTime date)
    {
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var reminder = new Reminder { UserId = userId, Reminder1 = content, Date = date };
        db.Reminders.Add(reminder);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return reminder;
    }
}
