using dotBento.EntityFramework.Entities;
using dotBento.WebApi.Controllers;
using dotBento.WebApi.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using dotBento.EntityFramework.Context;
using Microsoft.Extensions.Caching.Memory;
using dotBento.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Linq;

namespace dotBento.WebApi.Tests.Controllers;

public class ProfileControllerTests
{
    // Helpers for parameterized tests
    public static IEnumerable<object[]> InvalidHexCases() => new List<object[]>
    {
        new object[]
        {
            nameof(ProfileUpdateRequest.BackgroundColour),
            "Invalid BackgroundColour. Must be a hex colour like #1F2937."
        },
        new object[] { nameof(ProfileUpdateRequest.OverlayColour), "Invalid OverlayColour. Must be hex #RRGGBB." },
        new object[] { nameof(ProfileUpdateRequest.UsernameColour), "Invalid UsernameColour. Must be hex #RRGGBB." },
        new object[] { nameof(ProfileUpdateRequest.XpBarColour), "Invalid XpBarColour." },
    };

    public static IEnumerable<object[]> InvalidOpacityCases() => new List<object[]>
    {
        new object[]
        {
            nameof(ProfileUpdateRequest.BackgroundColourOpacity), "BackgroundColourOpacity must be between 0 and 100."
        },
        new object[] { nameof(ProfileUpdateRequest.OverlayOpacity), "OverlayOpacity must be between 0 and 100." },
        new object[] { nameof(ProfileUpdateRequest.XpBarOpacity), "XpBarOpacity must be between 0 and 100." },
        new object[] { nameof(ProfileUpdateRequest.XpText2Opacity), "XpText2Opacity must be between 0 and 100." },
    };

    private static void SetProperty<T>(ProfileUpdateRequest request, string propertyName, T value)
    {
        var prop = typeof(ProfileUpdateRequest).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(prop);
        prop.SetValue(request, value);
    }

    private static async Task SeedUser(BotDbContext context, long userId)
    {
        context.Users.Add(new User
        {
            UserId = userId,
            Discriminator = "0000",
            Xp = 0,
            Level = 0,
            Username = "TestUser"
        });
        await context.SaveChangesAsync();
    }

    private sealed class SingleContextFactory : IDbContextFactory<BotDbContext>
    {
        private readonly BotDbContext _ctx;
        public SingleContextFactory(BotDbContext ctx) => _ctx = ctx;
        public BotDbContext CreateDbContext()
        {
            return CreateNewContextSharingStore();
        }
        public Task<BotDbContext> CreateDbContextAsync(System.Threading.CancellationToken cancellationToken = default)
            => Task.FromResult(CreateNewContextSharingStore());

        private BotDbContext CreateNewContextSharingStore()
        {
            // Re-create a BotDbContext that points to the same InMemory database as the provided context
            var options = _ctx.GetService<Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptions>();
            var inMemoryExt = options.Extensions.OfType<Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal.InMemoryOptionsExtension>().First();

            var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();
            var newOptions = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<BotDbContext>()
                .UseInMemoryDatabase(inMemoryExt.StoreName)
                .Options;
            return new BotDbContext(configuration, newOptions);
        }
    }

    private static ProfileController CreateController(BotDbContext context)
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var factory = new SingleContextFactory(context);
        var service = new ProfileService(cache, factory);
        return new ProfileController(context, service);
    }

    [Fact]
    public async Task GetProfile_UserNotFound_Returns404()
    {
        await using var context = DbContextHelper.GetInMemoryDbContext();
        var controller = CreateController(context);

        var result = await controller.GetProfile(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetProfile_NotFound_Returns404()
    {
        await using var context = DbContextHelper.GetInMemoryDbContext();
        var controller = CreateController(context);
        await SeedUser(context, 123);

        var result = await controller.GetProfile(123);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetProfile_Found_ReturnsDto()
    {
        await using var context = DbContextHelper.GetInMemoryDbContext();
        context.Profiles.Add(new Profile
        {
            UserId = 1,
            LastfmBoard = true,
            XpBoard = false,
            BackgroundUrl = "https://img",
            BackgroundColourOpacity = 80,
            BackgroundColour = "#123456",
            Description = "Hi",
            Timezone = "Europe/Oslo",
            Birthday = "2000-07-21"
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        await SeedUser(context, 1);

        var result = await controller.GetProfile(1);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ProfileDto>(ok.Value);
        Assert.Equal(1, dto.UserId);
        Assert.True(dto.LastfmBoard);
        Assert.False(dto.XpBoard);
        Assert.Equal("https://img", dto.BackgroundUrl);
        Assert.Equal(80, dto.BackgroundColourOpacity);
        Assert.Equal("#123456", dto.BackgroundColour);
        Assert.Equal("Hi", dto.Description);
        Assert.Equal("Europe/Oslo", dto.Timezone);
        Assert.Equal("2000-07-21", dto.Birthday);
    }

    [Fact]
    public async Task UpsertProfile_CreatesNew_ReturnsDto_AndPersists()
    {
        await using var context = DbContextHelper.GetInMemoryDbContext();
        var controller = CreateController(context);

        await SeedUser(context, 5);

        var request = new ProfileUpdateRequest
        {
            UserId = 5,
            LastfmBoard = true,
            XpBoard = true,
            BackgroundUrl = "https://img.example/bg.png",
            BackgroundColour = "#1F2937",
            BackgroundColourOpacity = 90,
            Description = "Hello",
            Timezone = "Europe/Oslo",
            Birthday = "02-29"
        };

        var result = await controller.UpsertProfile(request);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ProfileDto>(ok.Value);
        Assert.Equal(5, dto.UserId);
        Assert.True(dto.LastfmBoard);
        Assert.True(dto.XpBoard);
        Assert.Equal("https://img.example/bg.png", dto.BackgroundUrl);
        Assert.Equal("#1F2937", dto.BackgroundColour);
        Assert.Equal(90, dto.BackgroundColourOpacity);
        Assert.Equal("Hello", dto.Description);
        Assert.Equal("Europe/Oslo", dto.Timezone);
        Assert.Equal("2000-02-29", dto.Birthday);

        // Persisted
        var entity = context.Profiles.First(p => p.UserId == 5);
        Assert.True(entity.LastfmBoard);
        Assert.True(entity.XpBoard);
    }

    [Fact]
    public async Task UpsertProfile_UpdatesExisting_ReturnsDto_AndPersists()
    {
        await using var context = DbContextHelper.GetInMemoryDbContext();
        context.Profiles.Add(new Profile
        {
            UserId = 10,
            LastfmBoard = false,
            XpBoard = false,
            Description = "Old",
            BackgroundColour = "#000000",
            BackgroundColourOpacity = 50
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        await SeedUser(context, 10);

        var request = new ProfileUpdateRequest
        {
            UserId = 10,
            LastfmBoard = true,
            Description = "New",
            BackgroundColourOpacity = 75
        };

        var result = await controller.UpsertProfile(request);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ProfileDto>(ok.Value);
        Assert.Equal(10, dto.UserId);
        Assert.True(dto.LastfmBoard);
        Assert.Equal("New", dto.Description);
        Assert.Equal(75, dto.BackgroundColourOpacity);
        // Unchanged fields
        Assert.False(dto.XpBoard);
        Assert.Equal("#000000", dto.BackgroundColour);

        var entity = await context.Profiles.AsNoTracking().FirstAsync(p => p.UserId == 10);
        Assert.True(entity.LastfmBoard);
        Assert.Equal("New", entity.Description);
        Assert.Equal(75, entity.BackgroundColourOpacity);
        Assert.False(entity.XpBoard);
        Assert.Equal("#000000", entity.BackgroundColour);
    }

    [Fact]
    public async Task UpsertProfile_InvalidBackgroundUrl_ReturnsBadRequest()
    {
        await using var context = DbContextHelper.GetInMemoryDbContext();
        var controller = CreateController(context);
        await SeedUser(context, 42);
        var result = await controller.UpsertProfile(new ProfileUpdateRequest
        {
            UserId = 42,
            BackgroundUrl = "ftp://not-allowed"
        });
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpsertProfile_InvalidHex_ReturnsBadRequest()
    {
        await using var context = DbContextHelper.GetInMemoryDbContext();
        var controller = CreateController(context);
        await SeedUser(context, 42);
        var result = await controller.UpsertProfile(new ProfileUpdateRequest
        {
            UserId = 42,
            BackgroundColour = "ZZZZZZ"
        });
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpsertProfile_InvalidOpacity_ReturnsBadRequest()
    {
        await using var context = DbContextHelper.GetInMemoryDbContext();
        var controller = CreateController(context);
        await SeedUser(context, 42);
        var result = await controller.UpsertProfile(new ProfileUpdateRequest
        {
            UserId = 42,
            BackgroundColourOpacity = 150
        });
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpsertProfile_InvalidTimezone_ReturnsBadRequest()
    {
        await using var context = DbContextHelper.GetInMemoryDbContext();
        var controller = CreateController(context);
        await SeedUser(context, 42);
        var result = await controller.UpsertProfile(new ProfileUpdateRequest
        {
            UserId = 42,
            Timezone = "Not/A_Real_Zone"
        });
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpsertProfile_InvalidBirthday_ReturnsBadRequest()
    {
        await using var context = DbContextHelper.GetInMemoryDbContext();
        var controller = CreateController(context);
        await SeedUser(context, 42);
        var result = await controller.UpsertProfile(new ProfileUpdateRequest
        {
            UserId = 42,
            Birthday = "13-40"
        });
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpsertProfile_NormalizesHexAndBirthday_Succeeds()
    {
        await using var context = DbContextHelper.GetInMemoryDbContext();
        var controller = CreateController(context);
        await SeedUser(context, 77);
        var result = await controller.UpsertProfile(new ProfileUpdateRequest
        {
            UserId = 77,
            BackgroundColour = "1f2937",
            Birthday = "7/21"
        });
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ProfileDto>(ok.Value);
        Assert.Equal("#1F2937", dto.BackgroundColour);
        Assert.Equal("2000-07-21", dto.Birthday);
    }

    [Fact]
    public async Task GetProfile_AdditionalFields_AreMapped()
    {
        await using var context = DbContextHelper.GetInMemoryDbContext();
        context.Profiles.Add(new Profile
        {
            UserId = 100,
            UsernameColour = "#ABCDEF",
            XpBar2Opacity = 50
        });
        await context.SaveChangesAsync();
        var controller = CreateController(context);
        await SeedUser(context, 100);
        var result = await controller.GetProfile(100);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ProfileDto>(ok.Value);
        Assert.Equal("#ABCDEF", dto.UsernameColour);
        Assert.Equal(50, dto.XpBar2Opacity);
    }

    [Theory]
    [MemberData(nameof(InvalidHexCases))]
    public async Task UpsertProfile_InvalidHexFields_ReturnsSpecificBadRequest(string propertyName,
        string expectedMessage)
    {
        await using var context = DbContextHelper.GetInMemoryDbContext();
        var controller = CreateController(context);
        await SeedUser(context, 4242);
        var req = new ProfileUpdateRequest { UserId = 4242 };
        SetProperty(req, propertyName, "ZZZZZZ");

        var result = await controller.UpsertProfile(req);
        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(expectedMessage, bad.Value);
    }

    [Theory]
    [MemberData(nameof(InvalidOpacityCases))]
    public async Task UpsertProfile_InvalidOpacityFields_ReturnsSpecificBadRequest(string propertyName,
        string expectedMessage)
    {
        await using var context = DbContextHelper.GetInMemoryDbContext();
        var controller = CreateController(context);
        await SeedUser(context, 4343);
        var req = new ProfileUpdateRequest { UserId = 4343 };
        SetProperty(req, propertyName, 150);

        var result = await controller.UpsertProfile(req);
        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(expectedMessage, bad.Value);
    }

    [Fact]
    public async Task UpsertProfile_NegativeSidebarBlur_ReturnsSpecificBadRequest()
    {
        await using var context = DbContextHelper.GetInMemoryDbContext();
        var controller = CreateController(context);
        await SeedUser(context, 45);
        var result = await controller.UpsertProfile(new ProfileUpdateRequest
        {
            UserId = 45,
            SidebarBlur = -1
        });
        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("SidebarBlur cannot be negative.", bad.Value);
    }

    [Fact]
    public async Task UpsertProfile_ValidHttpUrl_IsAccepted()
    {
        await using var context = DbContextHelper.GetInMemoryDbContext();
        var controller = CreateController(context);
        await SeedUser(context, 46);
        var result = await controller.UpsertProfile(new ProfileUpdateRequest
        {
            UserId = 46,
            BackgroundUrl = "http://example.com/img.png"
        });
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ProfileDto>(ok.Value);
        Assert.Equal("http://example.com/img.png", dto.BackgroundUrl);
    }

    [Fact]
    public async Task UpsertProfile_Normalizes_UsernameHex()
    {
        await using var context = DbContextHelper.GetInMemoryDbContext();
        var controller = CreateController(context);
        await SeedUser(context, 47);
        var result = await controller.UpsertProfile(new ProfileUpdateRequest
        {
            UserId = 47,
            UsernameColour = "abc123"
        });
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ProfileDto>(ok.Value);
        Assert.Equal("#ABC123", dto.UsernameColour);
    }
}