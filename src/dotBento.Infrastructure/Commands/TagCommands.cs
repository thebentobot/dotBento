using CSharpFunctionalExtensions;
using dotBento.Domain;
using dotBento.Domain.Entities.Tags;
using dotBento.Domain.Extensions;
using dotBento.Infrastructure.Services;
using dotBento.Infrastructure.Extensions;

namespace dotBento.Infrastructure.Commands;

public sealed class TagCommands(TagService tagService)
{
    public async Task<Result> CreateTagAsync(long userId, long guildId, string name, string content)
    {
        // TODO we want a better sensitive character check before launching
        if (Constants.CommandNames.Contains(name) || Constants.AliasNames.Contains(name) || name.ContainsSensitiveCharacters())
        {
            return Result.Failure("Tag name cannot include any characters, Bento command name or Bento command alias.");
        }
        var tagExistsCheck = await tagService.FindTagAsync(guildId, name);
        if (tagExistsCheck.HasValue)
        {
            return Result.Failure("Tag name already exists on this server.");
        }
        if (string.IsNullOrWhiteSpace(SanitizeTagContent(content)))
        {
            return Result.Failure("Tag content cannot be empty.");
        }
        var tag = await tagService.CreateTagAsync(userId, guildId, name, SanitizeTagContent(content));
        return Result.Success(tag);
    }
    
    public async Task<Result> DeleteTagAsync(long userId, long guildId, string name, bool hasMessageEditPerms)
    {
        var tagExistsCheck = await tagService.FindTagAsync(guildId, name);
        if (!tagExistsCheck.HasValue)
        {
            return Result.Failure("Tag not found.");
        }
        if (!hasMessageEditPerms || userId != tagExistsCheck.Value.UserId)
        {
            return Result.Failure("You can only delete your own tags.");
        }
        await tagService.DeleteTagAsync(userId, guildId, name);
        return Result.Success();
    }
    
    public async Task<Result> UpdateTagAsync(long userId, long guildId, string name, string content, bool hasMessageEditPerms)
    {
        if (string.IsNullOrWhiteSpace(SanitizeTagContent(content)))
        {
            return Result.Failure("Tag content cannot be empty.");
        }
        var tagExistsCheck = await tagService.FindTagAsync(guildId, name);
        if (!tagExistsCheck.HasValue)
        {
            return Result.Failure("Tag not found.");
        }
        if (!hasMessageEditPerms || userId != tagExistsCheck.Value.UserId)
        {
            return Result.Failure("You can only update your own tags.");
        }
        await tagService.UpdateTagAsync(userId, guildId, name, SanitizeTagContent(content));
        return Result.Success();
    }
    
    public async Task<Result> RenameTagAsync(long userId, long guildId, string oldName, string newName, bool hasMessageEditPerms)
    {
        var tagExistsCheck = await tagService.FindTagAsync(guildId, newName);
        if (Constants.CommandNames.Contains(newName) || Constants.AliasNames.Contains(newName) || tagExistsCheck.HasValue)
        {
            return Result.Failure("New tag name cannot be an existing tag, Bento command name or Bento command alias.");
        }
        if (string.IsNullOrWhiteSpace(newName))
        {
            return Result.Failure("New tag name cannot be empty.");
        }
        if (!hasMessageEditPerms || userId != tagExistsCheck.Value.UserId)
        {
            return Result.Failure("You can only rename your own tags.");
        }
        await tagService.RenameTagAsync(userId, guildId, oldName, newName);
        return Result.Success();
    }
    
    public async Task<Result<BentoTags>> FindTagAsync(long guildId, string name)
    {
        var tag = await tagService.FindTagAsync(guildId, name);
        return tag.HasValue ? Result.Success(tag.Value.ToBentoTag()) : Result.Failure<BentoTags>("Tag not found.");
    }
    
    public async Task<Result<List<BentoTags>>> FindTagsAsync(long guildId, bool top, Maybe<long> authorId)
    {
        var tags = await tagService.FindTagsAsync(guildId, top, authorId);
        return tags.IsSuccess
            ? Result.Success(tags.Value.Select(x => x.ToBentoTag()).ToList())
            : Result.Failure<List<BentoTags>>("No tags found.");
    }
    
    public async Task<Result<BentoTags>> GetRandomTagAsync(long userId, long guildId)
    {
        var tag = await tagService.GetRandomTagAsync(userId, guildId);
        return tag.HasValue ? Result.Success(tag.Value.ToBentoTag()) : Result.Failure<BentoTags>("No tags found.");
    }
    
    public async Task<Result<List<BentoTags>>> SearchTagsAsync(long guildId, string query)
    {
        var tagsByCommand = await tagService.SearchTagsByCommandAsync(guildId, query);
        var tagsByContent = await tagService.SearchTagsByContentAsync(guildId, query);
        var tags = tagsByCommand.Concat(tagsByContent).ToList();
        return tags.Count != 0
            ? Result.Success(tags.Select(x => x.ToBentoTag()).Distinct().ToList())
            : Result.Failure<List<BentoTags>>("No tags found.");
    }
    
    public async Task IncrementTagUsageAsync(long tagId)
    {
        await tagService.IncrementTagCountAsync(tagId);
    }

    private static string SanitizeTagContent(string content) =>
        content.FilterOutMentions().TrimToMaxLength(2000);
}