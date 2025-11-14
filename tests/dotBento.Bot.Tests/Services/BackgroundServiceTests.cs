using Discord;
using Discord.WebSocket;
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

    private static BackgroundService CreateSut(
        UserService userService,
        DiscordSocketClient client,
        ReminderCommands reminderCommands,
        IDbContextFactory<BotDbContext> contextFactory,
        IDiscordUserResolver userResolver,
        IDmSender dmSender)
    {
        // For this test suite, only userService, client, and reminderCommands are used by SendRemindersToUsers.
        // The other ctor args are not used in this method, so we can safely pass nulls.
        var guildService = (GuildService)null!;
        var supporterService = (SupporterService)null!;
        return new BackgroundService(userService, guildService, client, supporterService, reminderCommands, contextFactory, userResolver, dmSender);
    }

    [Fact]
    public async Task SendRemindersToUsers_WhenUserNotInDatabase_DeletesReminder()
    {
        // Arrange
        var dbFactory = new InMemoryDbFactory();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var userService = new UserService(cache, dbFactory);

        const long userId = 12345;
        var reminderId = await SeedReminderAsync(dbFactory, userId, DateTime.UtcNow.AddMinutes(-1));

        var reminderService = new ReminderService(cache, dbFactory);
        var reminderCommands = new ReminderCommands(reminderService);

        var clientMock = new Mock<DiscordSocketClient>(new DiscordSocketConfig());
        var resolverMock = new Mock<IDiscordUserResolver>();
        var dmSenderMock = new Mock<IDmSender>(MockBehavior.Strict);

        var sut = CreateSut(userService, clientMock.Object, reminderCommands, dbFactory, resolverMock.Object, dmSenderMock.Object);

        // Act
        await sut.SendRemindersToUsers();

        // Assert
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var deleted = await db.Reminders.FirstOrDefaultAsync(r => r.Id == reminderId);
            Assert.Null(deleted);
        }
        // We expect no Discord API lookups when the user isn't in our DB.
        resolverMock.Verify(r => r.GetUserAsync(It.IsAny<ulong>(), It.IsAny<RequestOptions?>()), Times.Never);
        dmSenderMock.Verify(s => s.SendReminderAsync(It.IsAny<IUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SendRemindersToUsers_WhenDiscordUserNull_DeletesReminder()
    {
        // Arrange
        var dbFactory = new InMemoryDbFactory();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var userService = new UserService(cache, dbFactory);

        const long userId = 22222;
        await SeedUserAsync(dbFactory, userId);

        var reminderId = await SeedReminderAsync(dbFactory, userId, DateTime.UtcNow.AddMinutes(-1));

        var reminderService = new ReminderService(cache, dbFactory);
        var reminderCommands = new ReminderCommands(reminderService);

        var clientMock = new Mock<DiscordSocketClient>(new DiscordSocketConfig());
        var resolverMock = new Mock<IDiscordUserResolver>();
        resolverMock.Setup(r => r.GetUserAsync(userId, It.IsAny<RequestOptions?>()))
            .Returns(ValueTask.FromResult((IUser?)null));

        var dmSenderMock = new Mock<IDmSender>(MockBehavior.Strict);

        var sut = CreateSut(userService, clientMock.Object, reminderCommands, dbFactory, resolverMock.Object, dmSenderMock.Object);

        // Act
        await sut.SendRemindersToUsers();

        // Assert
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var deleted = await db.Reminders.FirstOrDefaultAsync(r => r.Id == reminderId);
            Assert.Null(deleted);
        }
        resolverMock.Verify(r => r.GetUserAsync(userId, It.IsAny<RequestOptions?>()), Times.Once);
        dmSenderMock.Verify(s => s.SendReminderAsync(It.IsAny<IUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SendRemindersToUsers_WhenUnexpectedException_OmitsDeletion()
    {
        // Arrange
        var dbFactory = new InMemoryDbFactory();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var userService = new UserService(cache, dbFactory);

        const long userId = 33333;
        await SeedUserAsync(dbFactory, userId);

        var reminderId = await SeedReminderAsync(dbFactory, userId, DateTime.UtcNow.AddMinutes(-1));

        var reminderService = new ReminderService(cache, dbFactory);
        var reminderCommands = new ReminderCommands(reminderService);

        var channelUserMock = new Mock<IUser>(MockBehavior.Strict);

        var clientMock = new Mock<DiscordSocketClient>(new DiscordSocketConfig());
        var resolverMock = new Mock<IDiscordUserResolver>();
        resolverMock.Setup(r => r.GetUserAsync(userId, It.IsAny<RequestOptions?>()))
            .Returns(ValueTask.FromResult<IUser?>(channelUserMock.Object));

        var dmSenderMock = new Mock<IDmSender>(MockBehavior.Strict);
        dmSenderMock
            .Setup(s => s.SendReminderAsync(channelUserMock.Object, It.IsAny<string>()))
            .ThrowsAsync(new Exception("boom"));

        var sut = CreateSut(userService, clientMock.Object, reminderCommands, dbFactory, resolverMock.Object, dmSenderMock.Object);

        // Act
        await sut.SendRemindersToUsers();

        // Assert
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var stillThere = await db.Reminders.FirstOrDefaultAsync(r => r.Id == reminderId);
            Assert.NotNull(stillThere);
        }
        resolverMock.Verify(r => r.GetUserAsync(userId, It.IsAny<RequestOptions?>()), Times.Once);
        dmSenderMock.Verify(s => s.SendReminderAsync(channelUserMock.Object, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task SendRemindersToUsers_WhenDmForbidden_DeletesReminder()
    {
        // Arrange
        var dbFactory = new InMemoryDbFactory();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var userService = new UserService(cache, dbFactory);

        const long userId = 44444;
        await SeedUserAsync(dbFactory, userId);

        var reminderId = await SeedReminderAsync(dbFactory, userId, DateTime.UtcNow.AddMinutes(-1));

        var reminderService = new ReminderService(cache, dbFactory);
        var reminderCommands = new ReminderCommands(reminderService);

        var clientMock = new Mock<DiscordSocketClient>(new DiscordSocketConfig());
        var resolverMock = new Mock<IDiscordUserResolver>();
        var discordUserMock = new Mock<IUser>(MockBehavior.Strict);
        resolverMock.Setup(r => r.GetUserAsync(userId, It.IsAny<RequestOptions?>()))
            .Returns(ValueTask.FromResult<IUser?>(discordUserMock.Object));

        var dmSenderMock = new Mock<IDmSender>(MockBehavior.Strict);
        dmSenderMock
            .Setup(s => s.SendReminderAsync(discordUserMock.Object, It.IsAny<string>()))
            .ReturnsAsync(DmSendResult.Forbidden);

        var sut = CreateSut(userService, clientMock.Object, reminderCommands, dbFactory, resolverMock.Object, dmSenderMock.Object);

        // Act
        await sut.SendRemindersToUsers();

        // Assert
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var deleted = await db.Reminders.FirstOrDefaultAsync(r => r.Id == reminderId);
            Assert.Null(deleted);
        }
        resolverMock.Verify(r => r.GetUserAsync(userId, It.IsAny<RequestOptions?>()), Times.Once);
        dmSenderMock.Verify(s => s.SendReminderAsync(discordUserMock.Object, It.IsAny<string>()), Times.Once);
    }
}
