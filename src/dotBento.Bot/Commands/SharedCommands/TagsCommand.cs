using CSharpFunctionalExtensions;
using NetCord;
using NetCord.Rest;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Resources;
using dotBento.Infrastructure.Commands;
using dotBento.Infrastructure.Dto.Tags;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SharedCommands;

public sealed class TagsCommand(TagCommands tagCommands)
{
    public async Task<ResponseModel> CreateTagAsync(long userId, long guildId, string name, TagContentDto content)
    {
        var embed = new ResponseModel { ResponseType = ResponseType.Embed };
        if (content.MessageContent == null && content.Attachments.Length == 0)
        {
            embed.Embed
                .WithColor(new Color(255, 0, 0))
                .WithTitle("Error")
                .WithDescription("Tag content cannot be empty.\nPlease provide message content or attachment.");
            return embed;
        }
        var tagContent = string.Empty;
        if (content.MessageContent != null)
        {
            tagContent = content.MessageContent;
        }
        if (content.Attachments.Length > 0)
        {
            tagContent += "\n" + string.Join("\n", content.Attachments.Select(x => x?.Url));
        }
        if (name.Contains(' '))
        {
            embed.Embed
                .WithColor(new Color(255, 0, 0))
                .WithTitle("Error")
                .WithDescription("Tag name cannot contain spaces.");
            return embed;
        }
        var result = await tagCommands.CreateTagAsync(userId, guildId, name, tagContent);
        if (result.IsFailure)
        {
            embed.Embed
                .WithColor(new Color(255, 0, 0))
                .WithTitle("Error")
                .WithDescription(result.Error);
            return embed;
        }

        embed.Embed
            .WithColor(new Color(50, 205, 50))
            .WithTitle($"The tag \"{name}\" has been created successfully");
        return embed;
    }

    public async Task<ResponseModel> DeleteTagAsync(long userId, long guildId, string name, bool hasMessageEditPerms)
    {
        var embed = new ResponseModel { ResponseType = ResponseType.Embed };
        var result = await tagCommands.DeleteTagAsync(userId, guildId, name, hasMessageEditPerms);
        if (result.IsFailure)
        {
            embed.Embed
                .WithColor(new Color(255, 0, 0))
                .WithTitle("Error")
                .WithDescription(result.Error);
            return embed;
        }

        embed.Embed
            .WithColor(new Color(50, 205, 50))
            .WithTitle($"The tag \"{name}\" has been deleted successfully");
        return embed;
    }

    public async Task<ResponseModel> UpdateTagAsync(long userId, long guildId, string name, TagContentDto tagContent, bool hasMessageEditPerms)
    {
        var embed = new ResponseModel { ResponseType = ResponseType.Embed };
        if (tagContent.MessageContent == null && tagContent.Attachments.Length == 0)
        {
            embed.Embed
                .WithColor(new Color(255, 0, 0))
                .WithTitle("Error")
                .WithDescription("Tag content cannot be empty.\nPlease provide message content or attachment.");
            return embed;
        }
        var existingTag = await tagCommands.FindTagAsync(guildId, name);
        // TODO: ask the user if they want to create the tag if it doesn't exist
        if (existingTag.IsFailure)
        {
            embed.Embed
                .WithColor(new Color(255, 0, 0))
                .WithTitle("Error")
                .WithDescription(existingTag.Error);
            return embed;
        }
        var content = string.Empty;
        if (tagContent.MessageContent != null)
        {
            content = tagContent.MessageContent;
        }
        if (tagContent.Attachments.Length > 0)
        {
            content += "\n" + string.Join("\n", tagContent.Attachments.Select(x => x?.Url));
        }
        var result = await tagCommands.UpdateTagAsync(userId, guildId, name, content, hasMessageEditPerms);
        if (result.IsFailure)
        {
            embed.Embed
                .WithColor(new Color(255, 0, 0))
                .WithTitle("Error")
                .WithDescription(result.Error);
            return embed;
        }

        embed.Embed
            .WithColor(new Color(50, 205, 50))
            .WithTitle($"The tag \"{name}\" has been updated successfully");
        return embed;
    }

    public async Task<ResponseModel> RenameTagAsync(long userId, long guildId, string oldName, string newName, bool hasMessageEditPerms)
    {
        var embed = new ResponseModel { ResponseType = ResponseType.Embed };
        if (newName.Contains(' '))
        {
            embed.Embed
                .WithColor(new Color(255, 0, 0))
                .WithTitle("Error")
                .WithDescription("Tag name cannot contain spaces.");
            return embed;
        }
        var result = await tagCommands.RenameTagAsync(userId, guildId, oldName, newName, hasMessageEditPerms);
        if (result.IsFailure)
        {
            embed.Embed
                .WithColor(new Color(255, 0, 0))
                .WithTitle("Error")
                .WithDescription(result.Error);
            return embed;
        }

        embed.Embed
            .WithColor(new Color(50, 205, 50))
            .WithTitle($"The tag \"{oldName}\" has successfully been renamed to \"{newName}\"");
        return embed;
    }

    public async Task<ResponseModel> GetRandomTagAsync(long userId, long guildId)
    {
        var result = await tagCommands.GetRandomTagAsync(userId, guildId);
        if (result.IsFailure)
        {
            var errorEmbed = new ResponseModel { ResponseType = ResponseType.Embed };
            errorEmbed.Embed
                .WithColor(new Color(255, 0, 0))
                .WithTitle("Error")
                .WithDescription(result.Error);
            return errorEmbed;
        }
        var resultEmbed = new ResponseModel
        {
            ResponseType = ResponseType.Text,
            Text = result.Value.Content
        };
        await tagCommands.IncrementTagUsageAsync(result.Value.TagId);
        return resultEmbed;
    }

    public async Task<ResponseModel> FindTagAsync(long guildId, string name)
    {
        var result = await tagCommands.FindTagAsync(guildId, name);
        if (result.IsFailure)
        {
            var errorEmbed = new ResponseModel { ResponseType = ResponseType.Embed };
            errorEmbed.Embed
                .WithColor(new Color(255, 0, 0))
                .WithTitle("Error")
                .WithDescription(result.Error);
            return errorEmbed;
        }
        var resultEmbed = new ResponseModel
        {
            ResponseType = ResponseType.Text,
            Text = result.Value.Content
        };
        await tagCommands.IncrementTagUsageAsync(result.Value.TagId);
        return resultEmbed;
    }

    public async Task<Maybe<ResponseModel>> MaybeFindTagAsync(long guildId, string name)
    {
        var result = await tagCommands.FindTagAsync(guildId, name);
        if (result.IsFailure)
        {
            return Maybe<ResponseModel>.None;
        }
        var resultEmbed = new ResponseModel
        {
            ResponseType = ResponseType.Text,
            Text = result.Value.Content
        };
        await tagCommands.IncrementTagUsageAsync(result.Value.TagId);
        return resultEmbed;
    }

    public async Task<ResponseModel> ListTagsAsync(long guildId, bool top, Maybe<GuildUser> author)
    {
        var embed = new ResponseModel { ResponseType = ResponseType.Paginator };
        var authorId = author.HasValue ? (long)author.Value.Id : Maybe<long>.None;
        var result = await tagCommands.FindTagsAsync(guildId, top, authorId);
        if (result.IsFailure)
        {
            embed.Embed
                .WithColor(new Color(255, 0, 0))
                .WithTitle("Error")
                .WithDescription("Something went wrong while fetching tags. Please try again later.");
            embed.ResponseType = ResponseType.Embed;
            return embed;
        }
        if (result.Value.Count == 0)
        {
            embed.Embed
                .WithColor(DiscordConstants.BentoYellow)
                .WithTitle("No tags found")
                .WithDescription(author.HasValue
                    ? "This user has not created any tags on this server yet."
                    : "This server has no tags yet. Create one with `/tag create`.");
            embed.ResponseType = ResponseType.Embed;
            return embed;
        }
        var tags = result.Value;

        var tagsPageChunks = tags.ChunkBy(10);

        var title = top ? "Top Tags" : "Tags";

        var pages = tagsPageChunks
            .Select(tagsPageChunk =>
            {
                if (!author.HasValue)
                    return new PageBuilder().WithTitle(title)
                        .WithColor(DiscordConstants.BentoYellow)
                        .WithFooter($"{tags.Count} {(tags.Count != 1 ? "tags" : "tag")} found")
                        .WithDescription(string.Join("\n", tagsPageChunk.Select(x => x.Command)));
                {
                    var authorUser = author.Value;
                    var authorDisplayName = authorUser.Nickname ?? authorUser.GlobalName ?? authorUser.Username;
                    var authorAvatarUrl = authorUser.GetGuildAvatarUrl()?.ToString(1024) ?? authorUser.GetAvatarUrl()?.ToString(1024);
                    var embedAuthor = new EmbedAuthorProperties()
                        .WithName(authorDisplayName)
                        .WithIconUrl(authorAvatarUrl);
                    return new PageBuilder().WithTitle(title)
                        .WithAuthor(embedAuthor)
                        .WithColor(DiscordConstants.BentoYellow)
                        .WithDescription(string.Join("\n", tagsPageChunk.Select(x => x.Command)));
                }
            })
            .ToList();

        embed.StaticPaginator = pages.BuildSimpleStaticPaginator();
        embed.ResponseType = ResponseType.Paginator;

        return embed;
    }

    public async Task<ResponseModel> GetTagInfoAsync(long guildId, string name)
    {
        var embed = new ResponseModel { ResponseType = ResponseType.Embed };
        var result = await tagCommands.FindTagAsync(guildId, name);
        if (result.IsFailure)
        {
            embed.Embed
                .WithColor(new Color(255, 0, 0))
                .WithTitle("Error")
                .WithDescription(result.Error);
            return embed;
        }
        var tag = result.Value;
        var description = tag.Content + $"Created by <@{tag.UserId}>";
        if (tag.Date.HasValue)
        {
            var unixTimestamp = new DateTimeOffset(tag.Date.Value).ToUnixTimeSeconds();
            description += $"\n<t:{unixTimestamp}:R>";
        }

        description += tag.Count switch
        {
            1 => $"\nThis tag has been used {tag.Count} time.",
            _ => $"\nThis tag has been used {tag.Count} times."
        };

        embed.Embed
            .WithColor(DiscordConstants.BentoYellow)
            .WithTitle(tag.Command)
            .WithDescription(description);
        return embed;
    }

    public async Task<ResponseModel> SearchTagsAsync(long guildId, string query)
    {
        var embed = new ResponseModel { ResponseType = ResponseType.Paginator };
        var tagsResults = await tagCommands.SearchTagsAsync(guildId, query);
        if (tagsResults.IsFailure)
        {
            embed.Embed
                .WithColor(new Color(255, 0, 0))
                .WithTitle("Error")
                .WithDescription("Something went wrong while searching tags. Please try again later.");
            embed.ResponseType = ResponseType.Embed;
            return embed;
        }
        if (tagsResults.Value.Count == 0)
        {
            var anyTags = await tagCommands.FindTagsAsync(guildId, false, Maybe<long>.None);
            var hasAnyTags = anyTags.IsSuccess && anyTags.Value.Count > 0;
            embed.Embed
                .WithColor(DiscordConstants.BentoYellow)
                .WithTitle("No tags found")
                .WithDescription(hasAnyTags
                    ? $"No tags match \"{query}\" on this server."
                    : "This server has no tags yet. Create one with `/tag create`.");
            embed.ResponseType = ResponseType.Embed;
            return embed;
        }
        var tags = tagsResults.Value;

        var tagsPageChunks = tags.ChunkBy(10);

        var pages = tagsPageChunks
            .Select(tagsPageChunk =>
            {
                return new PageBuilder().WithTitle("Search Results")
                    .WithColor(DiscordConstants.BentoYellow)
                    .WithFooter($"{tags.Count} {(tags.Count != 1 ? "tags" : "tag")} found")
                    .WithDescription(string.Join("\n", tagsPageChunk.Select(x => x.Command)));
            })
            .ToList();

        embed.StaticPaginator = pages.BuildSimpleStaticPaginator();
        embed.ResponseType = ResponseType.Paginator;

        return embed;
    }
}
