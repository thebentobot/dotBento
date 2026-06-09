using CSharpFunctionalExtensions;
using dotBento.EntityFramework.Entities;
using dotBento.Infrastructure.Commands;
using dotBento.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;

namespace dotBento.Infrastructure.Tests.Commands;

public sealed class TagCommandsTests
{
    [Fact]
    public async Task CreateTagAsync_ValidatesAndCreatesTags()
    {
        var command = CreateCommand(out var factory);

        var created = await command.CreateTagAsync(100, 1, "hello", " hello @everyone ");
        var duplicate = await command.CreateTagAsync(100, 1, "hello", "again");
        var reserved = await command.CreateTagAsync(100, 1, "Ping", "content");
        var sensitive = await command.CreateTagAsync(100, 1, "bad/name", "content");
        var emptyContent = await command.CreateTagAsync(100, 1, "empty", "@everyone");

        Assert.True(created.IsSuccess);
        Assert.True(duplicate.IsFailure);
        Assert.Equal("Tag name already exists on this server.", duplicate.Error);
        Assert.True(reserved.IsFailure);
        Assert.True(sensitive.IsFailure);
        Assert.True(emptyContent.IsFailure);

        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var tag = Assert.Single(db.Tags);
        Assert.Equal("hello", tag.Command);
        Assert.Equal(" hello  ", tag.Content);
    }

    [Fact]
    public async Task FindListSearchRandomAndIncrement_ReturnMappedTags()
    {
        var command = CreateCommand(out var factory);
        await SeedTagsAsync(factory);

        var found = await command.FindTagAsync(1, "alpha");
        var missing = await command.FindTagAsync(1, "missing");
        var tags = await command.FindTagsAsync(1, top: false, Maybe<long>.None);
        var authorTags = await command.FindTagsAsync(1, top: false, 200);
        var autocomplete = await command.FindTagNamesForAutocompleteAsync(1, Maybe<long>.None, "a");
        var random = await command.GetRandomTagAsync(100, 1);
        await command.IncrementTagUsageAsync(found.Value.TagId);

        Assert.True(found.IsSuccess);
        Assert.Equal("alpha", found.Value.Command);
        Assert.True(missing.IsFailure);
        Assert.True(tags.IsSuccess);
        Assert.Equal(2, tags.Value.Count);
        Assert.True(authorTags.IsSuccess);
        Assert.Equal(200, Assert.Single(authorTags.Value).UserId);
        Assert.Contains("alpha", autocomplete);
        Assert.True(random.IsSuccess);
    }

    [Fact]
    public async Task DeleteAndUpdateTagAsync_ValidateOwnershipAndPersistChanges()
    {
        var command = CreateCommand(out var factory);
        await SeedTagsAsync(factory);

        var missingDelete = await command.DeleteTagAsync(100, 1, "missing", hasMessageEditPerms: true);
        var notOwnerDelete = await command.DeleteTagAsync(999, 1, "alpha", hasMessageEditPerms: true);
        var missingUpdate = await command.UpdateTagAsync(100, 1, "missing", "content", hasMessageEditPerms: true);
        var emptyUpdate = await command.UpdateTagAsync(100, 1, "alpha", "@here", hasMessageEditPerms: true);
        var notOwnerUpdate = await command.UpdateTagAsync(999, 1, "alpha", "content", hasMessageEditPerms: true);
        var updated = await command.UpdateTagAsync(100, 1, "alpha", "updated", hasMessageEditPerms: true);
        var deleted = await command.DeleteTagAsync(100, 1, "alpha", hasMessageEditPerms: true);
        var afterDelete = await command.FindTagAsync(1, "alpha");

        Assert.True(missingDelete.IsFailure);
        Assert.True(notOwnerDelete.IsFailure);
        Assert.True(missingUpdate.IsFailure);
        Assert.True(emptyUpdate.IsFailure);
        Assert.True(notOwnerUpdate.IsFailure);
        Assert.True(updated.IsSuccess);
        Assert.True(deleted.IsSuccess);
        Assert.True(afterDelete.IsFailure);
    }

    [Fact]
    public async Task RenameTagAsync_ValidatesOwnershipAndPersistsChanges()
    {
        var command = CreateCommand(out var factory);
        await SeedTagsAsync(factory);

        var existingName = await command.RenameTagAsync(100, 1, "alpha", "beta", hasMessageEditPerms: true);
        var reserved = await command.RenameTagAsync(100, 1, "alpha", "Ping", hasMessageEditPerms: true);
        var empty = await command.RenameTagAsync(100, 1, "alpha", "", hasMessageEditPerms: true);
        var missing = await command.RenameTagAsync(100, 1, "missing", "gamma", hasMessageEditPerms: true);
        var notOwner = await command.RenameTagAsync(999, 1, "alpha", "gamma", hasMessageEditPerms: true);
        var renamed = await command.RenameTagAsync(100, 1, "alpha", "gamma", hasMessageEditPerms: true);
        var oldName = await command.FindTagAsync(1, "alpha");
        var newName = await command.FindTagAsync(1, "gamma");

        Assert.True(existingName.IsFailure);
        Assert.True(reserved.IsFailure);
        Assert.True(empty.IsFailure);
        Assert.True(missing.IsFailure);
        Assert.True(notOwner.IsFailure);
        Assert.True(renamed.IsSuccess);
        Assert.True(oldName.IsFailure);
        Assert.True(newName.IsSuccess);
    }

    private static TagCommands CreateCommand(out InfrastructureTestDbFactory factory)
    {
        factory = new InfrastructureTestDbFactory();
        return new TagCommands(new TagService(new MemoryCache(new MemoryCacheOptions()), factory));
    }

    private static async Task SeedTagsAsync(InfrastructureTestDbFactory factory)
    {
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        db.Tags.AddRange(
            new Tag
            {
                GuildId = 1,
                UserId = 100,
                Command = "alpha",
                Content = "first content",
                Count = 1,
                Date = DateTime.UtcNow
            },
            new Tag
            {
                GuildId = 1,
                UserId = 200,
                Command = "beta",
                Content = "second content",
                Count = 2,
                Date = DateTime.UtcNow
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
