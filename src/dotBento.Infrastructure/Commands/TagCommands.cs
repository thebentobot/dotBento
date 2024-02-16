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
}