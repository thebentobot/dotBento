using CSharpFunctionalExtensions;
using dotBento.Infrastructure.Services;

namespace dotBento.Infrastructure.Commands;

public class TagCommands(TagService tagService)
{
    public async Task<Result> CreateTagAsync(long userId, long guildId, string name, string content)
    {
        // TODO: Validate content before making the tag, like checking for mentions, etc.
        var tag = await tagService.CreateTagAsync(userId, guildId, name, content);
        return Result.Success(tag);
    }
    
    public async Task<Result> DeleteTagAsync(long userId, long guildId, string name)
    {
        await tagService.DeleteTagAsync(userId, guildId, name);
        return Result.Success();
    }
    
    public async Task<Result> UpdateTagAsync(long userId, long guildId, string name, string content)
    {
        await tagService.UpdateTagAsync(userId, guildId, name, content);
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

    private static bool IsValidContent(string content)
    {
        
    }
}