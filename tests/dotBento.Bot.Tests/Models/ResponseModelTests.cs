using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models.Discord;
using Fergun.Interactive;

namespace dotBento.Bot.Tests.Models;

public sealed class ResponseModelTests
{
    [Fact]
    public void Constructor_AllowsTextResponseWithText()
    {
        var response = new ResponseModel(ResponseType.Text, text: "hello");

        Assert.Equal(ResponseType.Text, response.ResponseType);
        Assert.Equal("hello", response.Text);
    }

    [Fact]
    public void Constructor_RejectsPaginatorWithoutPaginator()
    {
        var exception = Assert.Throws<ArgumentException>(() => new ResponseModel(ResponseType.Paginator));

        Assert.Equal("StaticPaginator must not be null when ResponseType is Paginator", exception.Message);
    }

    [Fact]
    public void Constructor_AllowsPaginatorWithPaginator()
    {
        var paginator = new List<PageBuilder> { new PageBuilder().WithDescription("page") }
            .BuildSimpleStaticPaginator();

        var response = new ResponseModel(ResponseType.Paginator, staticPaginator: paginator);

        Assert.Same(paginator, response.StaticPaginator);
    }

    [Fact]
    public void Constructor_RejectsTextResponseWithoutText()
    {
        var exception = Assert.Throws<ArgumentException>(() => new ResponseModel(ResponseType.Text));

        Assert.Equal("Text must not be null or empty when ResponseType is Text", exception.Message);
    }

    [Theory]
    [InlineData(ResponseType.ImageOnly)]
    [InlineData(ResponseType.ImageWithEmbed)]
    public void Constructor_RejectsImageResponseWithoutStreamAndFileName(ResponseType responseType)
    {
        var exception = Assert.Throws<ArgumentException>(() => new ResponseModel(responseType));

        Assert.Equal("Stream and FileName must not be null when ResponseType is ImageWithEmbed or ImageOnly", exception.Message);
    }

    [Theory]
    [InlineData(ResponseType.ImageOnly)]
    [InlineData(ResponseType.ImageWithEmbed)]
    public void Constructor_AllowsImageResponseWithStreamAndFileName(ResponseType responseType)
    {
        using var stream = new MemoryStream([1, 2, 3]);

        var response = new ResponseModel(responseType, stream: stream, fileName: "image.png");

        Assert.Same(stream, response.Stream);
        Assert.Equal("image.png", response.FileName);
    }
}
