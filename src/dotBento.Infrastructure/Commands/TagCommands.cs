using CSharpFunctionalExtensions;
using dotBento.Domain;
using dotBento.Domain.Extensions;
using dotBento.Infrastructure.Services;

namespace dotBento.Infrastructure.Commands;

public class TagCommands(TagService tagService)
{
    public async Task<Result> CreateTagAsync(long userId, long guildId, string name, string content)
    {
        var tagExistsCheck = await tagService.FindTagAsync(userId, guildId, name);
        if (Constants.CommandNames.Contains(name) || Constants.AliasNames.Contains(name) || tagExistsCheck.HasValue)
        {
            return Result.Failure("Tag name cannot be an existing tag, Bento command name or Bento command alias.");
        }
        if (string.IsNullOrWhiteSpace(SanitizeTagContent(content)))
        {
            return Result.Failure("Tag content cannot be empty.");
        }
        var tag = await tagService.CreateTagAsync(userId, guildId, name, SanitizeTagContent(content));
        return Result.Success(tag);
    }
    
    public async Task<Result> DeleteTagAsync(long userId, long guildId, string name)
    {
        await tagService.DeleteTagAsync(userId, guildId, name);
        return Result.Success();
    }
    
    public async Task<Result> UpdateTagAsync(long userId, long guildId, string name, string content)
    {
        if (string.IsNullOrWhiteSpace(SanitizeTagContent(content)))
        {
            return Result.Failure("Tag content cannot be empty.");
        }
        await tagService.UpdateTagAsync(userId, guildId, name, content);
        return Result.Success();
    }
    
    public async Task<Result> RenameTagAsync(long userId, long guildId, string oldName, string newName)
    {
        var tagExistsCheck = await tagService.FindTagAsync(userId, guildId, newName);
        if (Constants.CommandNames.Contains(newName) || Constants.AliasNames.Contains(newName) || tagExistsCheck.HasValue)
        {
            return Result.Failure("New tag name cannot be an existing tag, Bento command name or Bento command alias.");
        }
        await tagService.RenameTagAsync(userId, guildId, oldName, newName);
        return Result.Success();
    }
    
    public async Task<Result> FindTagAsync(long userId, long guildId, string name)
    {
        var tag = await tagService.FindTagAsync(userId, guildId, name);
        return tag.HasValue ? Result.Success(tag.Value) : Result.Failure("Tag not found.");
    }
    
    public async Task<Result> FindTagsAsync(long userId, long guildId)
    {
        var tags = await tagService.FindTagsAsync(userId, guildId);
        return Result.Success(tags);
    }

    private static string SanitizeTagContent(string content) =>
        content.FilterOutMentions().TrimToMaxLength(2000);
}