using Discord;
using Discord.Commands;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models.Discord;
using Moq;

namespace dotBento.Bot.Tests.Extensions;

public sealed class CommandContextExtensionsTests
{
    [Fact]
    public async Task SendResponse_SendsTextResponseWithoutMentions()
    {
        var channel = new Mock<IMessageChannel>();
        var context = new Mock<ICommandContext>();
        context.SetupGet(c => c.Channel).Returns(channel.Object);

        var response = new ResponseModel
        {
            ResponseType = ResponseType.Text,
            Text = "hello"
        };

        await context.Object.SendResponse(interactiveService: null!, response);

        channel.Verify(c => c.SendMessageAsync(
            "hello",
            false,
            null,
            null,
            AllowedMentions.None,
            null,
            null,
            null,
            null,
            MessageFlags.None,
            null), Times.Once);
    }

    [Fact]
    public async Task SendResponse_SendsEmbedResponse()
    {
        var channel = new Mock<IMessageChannel>();
        var context = new Mock<ICommandContext>();
        context.SetupGet(c => c.Channel).Returns(channel.Object);

        var response = new ResponseModel { ResponseType = ResponseType.Embed };
        response.Embed.WithTitle("Embed response");

        await context.Object.SendResponse(interactiveService: null!, response);

        channel.Verify(c => c.SendMessageAsync(
            "",
            false,
            It.Is<Embed>(embed => embed.Title == "Embed response"),
            null,
            null,
            null,
            null,
            null,
            null,
            MessageFlags.None,
            null), Times.Once);
    }

    [Fact]
    public async Task SendResponse_DisposesImageOnlyStreamAfterSending()
    {
        await using var stream = new MemoryStream([1, 2, 3]);
        var channel = new Mock<IMessageChannel>();
        var context = new Mock<ICommandContext>();
        context.SetupGet(c => c.Channel).Returns(channel.Object);

        var response = new ResponseModel
        {
            ResponseType = ResponseType.ImageOnly,
            FileName = "image",
            Stream = stream
        };

        await context.Object.SendResponse(interactiveService: null!, response);

        channel.Verify(c => c.SendFileAsync(
            stream: It.Is<Stream>(s => ReferenceEquals(s, stream)),
            filename: "image.png",
            text: null,
            isTTS: false,
            embed: null,
            options: null,
            isSpoiler: false,
            allowedMentions: null,
            messageReference: null,
            components: null,
            stickers: null,
            embeds: null,
            flags: MessageFlags.None,
            poll: null), Times.Once);
        Assert.Throws<ObjectDisposedException>(() => stream.Length);
    }
}
