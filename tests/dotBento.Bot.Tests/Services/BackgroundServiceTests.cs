using System.Runtime.CompilerServices;
using NetCord;
using dotBento.Bot.Models;
using dotBento.Bot.Services;
using dotBento.EntityFramework.Context;
using EfReminder = dotBento.EntityFramework.Entities.Reminder;
using EfUser = dotBento.EntityFramework.Entities.User;
using dotBento.Infrastructure.Commands;
using dotBento.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;

namespace dotBento.Bot.Tests.Services;

public sealed class BackgroundServiceTests
{
    private sealed class InMemoryDbFactory(string? dbName = null, InMemoryDatabaseRoot? root = null)
        : IDbContextFactory<BotDbContext>
    {
        private readonly string _dbName = dbName ?? Guid.NewGuid().ToString("N");
        private readonly InMemoryDatabaseRoot _root = root ?? new InMemoryDatabaseRoot();
        private readonly IConfiguration _config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

        public BotDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<BotDbContext>()
                .UseInMemoryDatabase(_dbName, _root)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new BotDbContext(_config, options);
        }

        public Task<BotDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateDbContext());
        }
    }

    private static async Task SeedUserAsync(IDbContextFactory<BotDbContext> factory, long userId)
    {
        await using var db = await factory.CreateDbContextAsync();
        db.Users.Add(new EfUser
        {
            UserId = userId,
            Discriminator = "User",
            Username = "tester",
            Level = 1,
            Xp = 0
        });
        await db.SaveChangesAsync();
    }

    private static async Task<int> SeedReminderAsync(IDbContextFactory<BotDbContext> factory, long userId, DateTime whenUtc)
    {
        await using var db = await factory.CreateDbContextAsync();
        var entity = new EfReminder
        {
            UserId = userId,
            Date = whenUtc,
            Reminder1 = "Test reminder"
        };
        db.Reminders.Add(entity);
        await db.SaveChangesAsync();
        return entity.Id;
    }

    // Creates a fake non-null User without calling its constructor.
    // Only used to pass the "user is null" check — no properties are accessed on the returned instance.
    private static User CreateFakeUser() =>
        (User)RuntimeHelpers.GetUninitializedObject(typeof(User));

    private static BackgroundService CreateSut(
        UserService userService,
        ReminderCommands reminderCommands,
        IDbContextFactory<BotDbContext> contextFactory,
        IDiscordUserResolver userResolver,
        IDmSender dmSender)
    {
        var guildService = (GuildService)null!;
        var supporterService = (SupporterService)null!;
        var botListService = (BotListService)null!;
        var botSettings = new Mock<IOptions<BotEnvConfig>>();
        botSettings.Setup(s => s.Value).Returns(new BotEnvConfig { Environment = "local" });
        return new BackgroundService(userService, guildService, null!, supporterService, botListService, reminderCommands, contextFactory, userResolver, dmSender, botSettings.Object);
    }

    [Fact]
    public async Task SendRemindersToUsers_WhenUserNotInDatabase_DeletesReminder()
    {
        // Arrange
        var dbFactory = new InMemoryDbFactory();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var userService = new UserService(cache, dbFactory);

        const ulong discordUserId = 232584569289703424UL;
        var reminderId = await SeedReminderAsync(dbFactory, (long)discordUserId, DateTime.UtcNow.AddMinutes(-1));

        var reminderService = new ReminderService(cache, dbFactory);
        var reminderCommands = new ReminderCommands(reminderService);

        var resolverMock = new Mock<IDiscordUserResolver>();
        var dmSenderMock = new Mock<IDmSender>(MockBehavior.Strict);

        var sut = CreateSut(userService, reminderCommands, dbFactory, resolverMock.Object, dmSenderMock.Object);

        // Act
        await sut.SendRemindersToUsers();

        // Assert
        await using (var db = await dbFactory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            var deleted = await db.Reminders.FirstOrDefaultAsync(r => r.Id == reminderId, cancellationToken: TestContext.Current.CancellationToken);
            Assert.Null(deleted);
        }
        // We expect no Discord API lookups when the user isn't in our DB.
        resolverMock.Verify(r => r.GetUserAsync(It.IsAny<ulong>()), Times.Never);
        dmSenderMock.Verify(s => s.SendReminderAsync(It.IsAny<ulong>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SendRemindersToUsers_WhenDiscordUserNull_DeletesReminder()
    {
        // Arrange
        var dbFactory = new InMemoryDbFactory();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var userService = new UserService(cache, dbFactory);

        const ulong discordUserId = 395743869201203200UL;
        await SeedUserAsync(dbFactory, (long)discordUserId);

        var reminderId = await SeedReminderAsync(dbFactory, (long)discordUserId, DateTime.UtcNow.AddMinutes(-1));

        var reminderService = new ReminderService(cache, dbFactory);
        var reminderCommands = new ReminderCommands(reminderService);

        var resolverMock = new Mock<IDiscordUserResolver>();
        resolverMock.Setup(r => r.GetUserAsync(discordUserId))
            .Returns(ValueTask.FromResult<User?>(null));

        var dmSenderMock = new Mock<IDmSender>(MockBehavior.Strict);

        var sut = CreateSut(userService, reminderCommands, dbFactory, resolverMock.Object, dmSenderMock.Object);

        // Act
        await sut.SendRemindersToUsers();

        // Assert
        await using (var db = await dbFactory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            var deleted = await db.Reminders.FirstOrDefaultAsync(r => r.Id == reminderId, cancellationToken: TestContext.Current.CancellationToken);
            Assert.Null(deleted);
        }
        resolverMock.Verify(r => r.GetUserAsync(discordUserId), Times.Once);
        dmSenderMock.Verify(s => s.SendReminderAsync(It.IsAny<ulong>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SendRemindersToUsers_WhenUnexpectedException_OmitsDeletion()
    {
        // Arrange
        var dbFactory = new InMemoryDbFactory();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var userService = new UserService(cache, dbFactory);

        const ulong discordUserId = 517219876543210496UL;
        await SeedUserAsync(dbFactory, (long)discordUserId);

        var reminderId = await SeedReminderAsync(dbFactory, (long)discordUserId, DateTime.UtcNow.AddMinutes(-1));

        var reminderService = new ReminderService(cache, dbFactory);
        var reminderCommands = new ReminderCommands(reminderService);

        var resolverMock = new Mock<IDiscordUserResolver>();
        resolverMock.Setup(r => r.GetUserAsync(discordUserId))
            .Returns(ValueTask.FromResult<User?>(CreateFakeUser()));

        var dmSenderMock = new Mock<IDmSender>(MockBehavior.Strict);
        dmSenderMock
            .Setup(s => s.SendReminderAsync(discordUserId, It.IsAny<string>()))
            .ThrowsAsync(new Exception("boom"));

        var sut = CreateSut(userService, reminderCommands, dbFactory, resolverMock.Object, dmSenderMock.Object);

        // Act
        await sut.SendRemindersToUsers();

        // Assert
        await using (var db = await dbFactory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            var stillThere = await db.Reminders.FirstOrDefaultAsync(r => r.Id == reminderId, cancellationToken: TestContext.Current.CancellationToken);
            Assert.NotNull(stillThere);
        }
        resolverMock.Verify(r => r.GetUserAsync(discordUserId), Times.Once);
        dmSenderMock.Verify(s => s.SendReminderAsync(discordUserId, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task SendRemindersToUsers_WhenDmForbidden_DeletesReminder()
    {
        // Arrange
        var dbFactory = new InMemoryDbFactory();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var userService = new UserService(cache, dbFactory);

        const ulong discordUserId = 684932157480309760UL;
        await SeedUserAsync(dbFactory, (long)discordUserId);

        var reminderId = await SeedReminderAsync(dbFactory, (long)discordUserId, DateTime.UtcNow.AddMinutes(-1));

        var reminderService = new ReminderService(cache, dbFactory);
        var reminderCommands = new ReminderCommands(reminderService);

        var resolverMock = new Mock<IDiscordUserResolver>();
        resolverMock.Setup(r => r.GetUserAsync(discordUserId))
            .Returns(ValueTask.FromResult<User?>(CreateFakeUser()));

        var dmSenderMock = new Mock<IDmSender>(MockBehavior.Strict);
        dmSenderMock
            .Setup(s => s.SendReminderAsync(discordUserId, It.IsAny<string>()))
            .ReturnsAsync(DmSendResult.Forbidden);

        var sut = CreateSut(userService, reminderCommands, dbFactory, resolverMock.Object, dmSenderMock.Object);

        // Act
        await sut.SendRemindersToUsers();

        // Assert
        await using (var db = await dbFactory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            var deleted = await db.Reminders.FirstOrDefaultAsync(r => r.Id == reminderId, cancellationToken: TestContext.Current.CancellationToken);
            Assert.Null(deleted);
        }
        resolverMock.Verify(r => r.GetUserAsync(discordUserId), Times.Once);
        dmSenderMock.Verify(s => s.SendReminderAsync(discordUserId, It.IsAny<string>()), Times.Once);
    }
}
