using System.Net;
using Discord;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Resources;
using dotBento.Infrastructure.Utilities;
using Moq;
using Serilog;
using SkiaSharp;

namespace dotBento.Bot.Tests.Commands.SharedCommands;

public sealed class AvatarCommandTests
{
    [Fact]
    public async Task UserAvatarCommand_UsesGlobalAvatar_InsteadOfGuildDisplayAvatar()
    {
        const string globalAvatarUrl = "https://cdn.example.com/global-avatar.png";
        var user = new Mock<IGuildUser>();
        user.SetupGet(x => x.GlobalName).Returns("Example");
        user.Setup(x => x.GetAvatarUrl(ImageFormat.WebP, 128))
            .Returns("https://cdn.example.com/global-avatar.webp");
        user.Setup(x => x.GetAvatarUrl(ImageFormat.Auto, 2048)).Returns(globalAvatarUrl);
        user.Setup(x => x.GetDisplayAvatarUrl(ImageFormat.WebP, 128))
            .Returns("https://cdn.example.com/server-avatar.webp");
        user.Setup(x => x.GetDisplayAvatarUrl(ImageFormat.Auto, 2048))
            .Returns("https://cdn.example.com/server-avatar.png");
        var command = CreateCommand(_ => ImageResponse());

        var response = await command.UserAvatarCommand(user.Object);
        var embed = response.Embed.Build();

        Assert.Equal(globalAvatarUrl, embed.Image?.Url);
    }

    [Fact]
    public async Task UserAvatarCommand_UsesDefaultAvatar_WhenUserHasNoGlobalAvatar()
    {
        const string defaultAvatarUrl = "https://cdn.example.com/default-avatar.png";
        var user = new Mock<IGuildUser>();
        user.SetupGet(x => x.GlobalName).Returns("Example");
        user.Setup(x => x.GetAvatarUrl(ImageFormat.WebP, 128)).Returns((string)null!);
        user.Setup(x => x.GetAvatarUrl(ImageFormat.Auto, 2048)).Returns((string)null!);
        user.Setup(x => x.GetDefaultAvatarUrl()).Returns(defaultAvatarUrl);
        user.Setup(x => x.GetDisplayAvatarUrl(ImageFormat.WebP, 128))
            .Returns("https://cdn.example.com/server-avatar.webp");
        user.Setup(x => x.GetDisplayAvatarUrl(ImageFormat.Auto, 2048))
            .Returns("https://cdn.example.com/server-avatar.png");
        var command = CreateCommand(_ => ImageResponse());

        var response = await command.UserAvatarCommand(user.Object);
        var embed = response.Embed.Build();

        Assert.Equal(defaultAvatarUrl, embed.Image?.Url);
    }

    [Fact]
    public async Task UserAvatarCommand_UsesFallbackColour_WhenColourExtractionFails()
    {
        var user = new Mock<IUser>();
        user.SetupGet(x => x.GlobalName).Returns("Example");
        user.Setup(x => x.GetAvatarUrl(ImageFormat.WebP, 128))
            .Returns("https://cdn.example.com/avatar.webp");
        user.Setup(x => x.GetAvatarUrl(ImageFormat.Auto, 2048))
            .Returns("https://cdn.example.com/avatar.png");
        var command = CreateCommand(_ => new HttpResponseMessage(HttpStatusCode.BadGateway));

        var response = await command.UserAvatarCommand(user.Object);
        var embed = response.Embed.Build();

        Assert.Equal(DiscordConstants.BentoYellow, embed.Color);
        Assert.Equal("https://cdn.example.com/avatar.png", embed.Image?.Url);
    }

    [Fact]
    public async Task ServerAvatarCommand_PrefersGuildAvatar()
    {
        const string guildAvatarUrl = "https://cdn.example.com/guild-avatar.png";
        var user = new Mock<IGuildUser>();
        user.SetupGet(x => x.Nickname).Returns("Server Name");
        user.Setup(x => x.GetGuildAvatarUrl(ImageFormat.WebP, 128))
            .Returns("https://cdn.example.com/guild-avatar.webp");
        user.Setup(x => x.GetGuildAvatarUrl(ImageFormat.Auto, 2048)).Returns(guildAvatarUrl);
        var command = CreateCommand(_ => ImageResponse());

        var response = await command.ServerAvatarCommand(user.Object);
        var embed = response.Embed.Build();

        Assert.Equal(guildAvatarUrl, embed.Image?.Url);
    }

    private static AvatarCommand CreateCommand(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(responseFactory));
        var silentLogger = new LoggerConfiguration().MinimumLevel.Fatal().CreateLogger();
        return new AvatarCommand(new StylingUtilities(httpClient), silentLogger);
    }

    private static HttpResponseMessage ImageResponse()
    {
        using var bitmap = new SKBitmap(2, 2);
        bitmap.Erase(SKColors.CornflowerBlue);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(data.ToArray())
        };
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory(request));
    }
}
