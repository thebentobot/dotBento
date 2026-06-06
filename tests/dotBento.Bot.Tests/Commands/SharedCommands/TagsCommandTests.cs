using Discord;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Enums;
using dotBento.EntityFramework.Entities;
using dotBento.Infrastructure.Commands;
using dotBento.Infrastructure.Dto.Tags;
using dotBento.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace dotBento.Bot.Tests.Commands.SharedCommands;

public sealed class TagsCommandTests
{
    private static (TagsCommand Command, TestDbFactory Factory) CreateCommand()
    {
        var factory = new TestDbFactory();
        var command = new TagsCommand(new TagCommands(new TagService(new MemoryCache(new MemoryCacheOptions()), factory)));
        return (command, factory);
    }

    [Fact]
    public async Task CreateTagAsync_ValidatesContentAndName()
    {
        var (command, _) = CreateCommand();

        var empty = await command.CreateTagAsync(10, 100, "tag", new TagContentDto(null, []));
        var space = await command.CreateTagAsync(10, 100, "bad tag", new TagContentDto("content", []));
        var created = await command.CreateTagAsync(10, 100, "tag", new TagContentDto("content", []));

        Assert.Equal("Error", empty.Embed.Build().Title);
        Assert.Equal("Error", space.Embed.Build().Title);
        Assert.Equal("The tag \"tag\" has been created successfully", created.Embed.Build().Title);
    }

    [Fact]
    public async Task CreateTagAsync_IncludesAttachmentUrls()
    {
        var (command, factory) = CreateCommand();
        var attachment = new Mock<IAttachment>();
        attachment.SetupGet(a => a.Url).Returns("https://example.com/file.png");

        await command.CreateTagAsync(10, 100, "tag", new TagContentDto("content", [attachment.Object]));

        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Contains("https://example.com/file.png", db.Tags.Single().Content);
    }

    [Fact]
    public async Task FindRandomAndMaybeFindTagAsync_ReturnTextAndIncrementCount()
    {
        var (command, factory) = CreateCommand();
        await SeedTagAsync(factory);

        var found = await command.FindTagAsync(100, "tag");
        var maybeFound = await command.MaybeFindTagAsync(100, "tag");
        var random = await command.GetRandomTagAsync(10, 100);
        var missing = await command.FindTagAsync(100, "missing");

        Assert.Equal(ResponseType.Text, found.ResponseType);
        Assert.True(maybeFound.HasValue);
        Assert.Equal(ResponseType.Text, random.ResponseType);
        Assert.Equal("Error", missing.Embed.Build().Title);
    }

    [Fact]
    public async Task UpdateRenameAndDeleteTagAsync_ReturnSuccessAndErrorResponses()
    {
        var (command, factory) = CreateCommand();
        await SeedTagAsync(factory);

        var updated = await command.UpdateTagAsync(10, 100, "tag", new TagContentDto("updated", []), true);
        var updateMissing = await command.UpdateTagAsync(10, 100, "missing", new TagContentDto("updated", []), true);
        var renamed = await command.RenameTagAsync(10, 100, "tag", "renamed", true);
        var invalidRename = await command.RenameTagAsync(10, 100, "renamed", "bad tag", true);
        var deleted = await command.DeleteTagAsync(10, 100, "renamed", true);
        var deleteMissing = await command.DeleteTagAsync(10, 100, "renamed", true);

        Assert.Equal("The tag \"tag\" has been updated successfully", updated.Embed.Build().Title);
        Assert.Equal("Error", updateMissing.Embed.Build().Title);
        Assert.Equal("The tag \"tag\" has successfully been renamed to \"renamed\"", renamed.Embed.Build().Title);
        Assert.Equal("Error", invalidRename.Embed.Build().Title);
        Assert.Equal("The tag \"renamed\" has been deleted successfully", deleted.Embed.Build().Title);
        Assert.Equal("Error", deleteMissing.Embed.Build().Title);
    }

    [Fact]
    public async Task ListAndInfoTagsAsync_ReturnExpectedResponses()
    {
        var (command, factory) = CreateCommand();
        await SeedTagAsync(factory);

        var list = await command.ListTagsAsync(100, top: false, CSharpFunctionalExtensions.Maybe<Discord.WebSocket.SocketGuildUser>.None);
        var info = await command.GetTagInfoAsync(100, "tag");

        Assert.Equal(ResponseType.Paginator, list.ResponseType);
        Assert.Equal("tag", info.Embed.Build().Title);
    }

    private static async Task SeedTagAsync(TestDbFactory factory)
    {
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        db.Tags.Add(new Tag
        {
            GuildId = 100,
            UserId = 10,
            Command = "tag",
            Content = "content",
            Count = 1,
            Date = DateTime.UtcNow
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
