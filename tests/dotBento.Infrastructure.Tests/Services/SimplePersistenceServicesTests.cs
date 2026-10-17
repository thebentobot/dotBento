using dotBento.EntityFramework.Entities;
using dotBento.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dotBento.Infrastructure.Tests.Services;

public class SimplePersistenceServicesTests
{
    [Fact]
    public async Task SupporterService_CountsAndCachesPatreonUsers()
    {
        var factory = new InfrastructureTestDbFactory();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Users.AddRange(User(1), User(2));
            db.Patreons.AddRange(
                new Patreon { UserId = 1, Name = "One", Avatar = "one.png" },
                new Patreon { UserId = 2, Name = "Two", Avatar = "two.png" });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var service = new SupporterService(factory, cache);

        var count = await service.GetActiveSupporterCountAsync();
        var found = await service.GetPatreonAsync(1);
        var cached = await service.GetPatreonAsync(1);
        var missing = await service.GetPatreonAsync(999);

        Assert.Equal(2, count);
        Assert.True(found.HasValue);
        Assert.True(cached.HasValue);
        Assert.Equal("One", cached.Value.Name);
        Assert.True(missing.HasNoValue);
    }

    [Fact]
    public async Task WeatherService_SavesUpdatesGetsAndDeletesWeather()
    {
        var factory = new InfrastructureTestDbFactory();
        var service = new WeatherService(factory);

        var missing = await service.GetWeatherAsync(10);
        await service.SaveWeatherAsync(10, "Copenhagen");
        await service.SaveWeatherAsync(10, "Oslo");
        var found = await service.GetWeatherAsync(10);
        await service.DeleteWeatherAsync(999);
        await service.DeleteWeatherAsync(10);
        var deleted = await service.GetWeatherAsync(10);

        Assert.True(missing.HasNoValue);
        Assert.True(found.HasValue);
        Assert.Equal("Oslo", found.Value.City);
        Assert.True(deleted.HasNoValue);
    }

    [Fact]
    public async Task HoroscopeService_SavesNormalizesUpdatesGetsAndDeletesSign()
    {
        var factory = new InfrastructureTestDbFactory();
        var service = new HoroscopeService(factory);

        var missing = await service.GetHoroscopeAsync(10);
        await service.SaveHoroscopeAsync(10, "  ARIES ");
        await service.SaveHoroscopeAsync(10, "Libra");
        var found = await service.GetHoroscopeAsync(10);
        await service.DeleteHoroscopeAsync(999);
        await service.DeleteHoroscopeAsync(10);
        var deleted = await service.GetHoroscopeAsync(10);

        Assert.True(missing.HasNoValue);
        Assert.True(found.HasValue);
        Assert.Equal("libra", found.Value.Sign);
        Assert.True(deleted.HasNoValue);
    }

    [Fact]
    public async Task LastFmService_SavesUpdatesGetsAndDeletesUsername()
    {
        var factory = new InfrastructureTestDbFactory();
        var service = new LastFmService(factory);

        var missing = await service.GetLastFmAsync(10);
        await service.SaveLastFmAsync(10, "first");
        await service.SaveLastFmAsync(10, "second");
        var found = await service.GetLastFmAsync(10);
        await service.DeleteLastFmAsync(999);
        await service.DeleteLastFmAsync(10);
        var deleted = await service.GetLastFmAsync(10);

        Assert.True(missing.HasNoValue);
        Assert.True(found.HasValue);
        Assert.Equal("second", found.Value.Lastfm1);
        Assert.True(deleted.HasNoValue);
    }

    [Fact]
    public async Task ReminderService_CreatesGetsUpdatesListsAndDeletesReminders()
    {
        var factory = new InfrastructureTestDbFactory();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new ReminderService(cache, factory);
        var reminderDate = DateTimeOffset.UtcNow.AddHours(1);

        var created = await service.CreateReminderAsync(10, "first", reminderDate);
        var byId = await service.GetReminderAsync(10, created.Id);
        var byContent = await service.GetReminderAsync(10, "first", reminderDate);
        var allForUser = await service.GetRemindersAsync(10);
        await service.UpdateReminderAsync(10, created.Id, "updated", reminderDate.AddHours(1));
        var updated = await service.GetReminderAsync(10, created.Id);
        await service.UpdateReminderAsync(10, 999, "missing", reminderDate);
        await service.DeleteReminderAsync(10, 999);
        await service.DeleteReminderAsync(10, created.Id);
        var deleted = await service.GetReminderAsync(10, created.Id);

        Assert.True(byId.HasValue);
        Assert.True(byContent.HasValue);
        Assert.Single(allForUser);
        Assert.True(updated.HasValue);
        Assert.Equal("updated", updated.Value.Reminder1);
        Assert.True(deleted.HasNoValue);
    }

    [Fact]
    public async Task ReminderService_ReturnsRecentReminders()
    {
        var factory = new InfrastructureTestDbFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Reminders.AddRange(
                new Reminder { UserId = 1, Reminder1 = "past", Date = DateTime.UtcNow.AddMinutes(-1) },
                new Reminder { UserId = 1, Reminder1 = "future", Date = DateTime.UtcNow.AddMinutes(10) });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var service = new ReminderService(new MemoryCache(new MemoryCacheOptions()), factory);

        var recent = await service.GetAllRecentRemindersAsync();

        var reminder = Assert.Single(recent);
        Assert.Equal("past", reminder.Reminder1);
    }

    private static User User(long id) => new()
    {
        UserId = id,
        Username = $"User{id}",
        Discriminator = "0001",
        Level = 1,
        Xp = 0
    };
}
