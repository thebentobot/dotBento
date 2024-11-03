using CSharpFunctionalExtensions;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Attributes;
using dotBento.Bot.AutoCompleteHandlers;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using dotBento.Infrastructure.Dto.Tags;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

[Group("tag", "Tags for the server")]
public sealed class TagsSlashCommand(InteractiveService interactiveService, TagsCommand tagsCommand)
    : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("get", "Get a tag")]
    [GuildOnly]
    public async Task GetCommand(
        [Summary("name", "Write a tag name")] [Autocomplete(typeof(SearchTagsAutoComplete))] string name,
        [Summary("hide", "Only the result of the message to you")] bool? hide = false
    ) =>
        await Context.SendResponse(
            interactiveService,
            await tagsCommand.FindTagAsync((long)Context.Guild.Id, name),
            hide ?? false
        );
    
    [SlashCommand("random", "Get a random tag")]
    [GuildOnly]
    public async Task RandomCommand(
        [Summary("hide", "Only the result of the message to you")] bool? hide = false
    ) =>
        await Context.SendResponse(
            interactiveService,
            await tagsCommand.GetRandomTagAsync((long)Context.User.Id, (long)Context.Guild.Id),
            hide ?? false
        );
    
    [SlashCommand("search", "Search for a tag")]
    [GuildOnly]
    public async Task SearchCommand(
        [Summary("query", "Search for a tag by name and content")] string query,
        [Summary("hide", "Only the result of the message to you")] bool? hide = false
    ) =>
        await Context.SendResponse(
            interactiveService,
            await tagsCommand.SearchTagsAsync((long)Context.Guild.Id, query),
            hide ?? false
        );
    
    [SlashCommand("create", "Create a tag")]
    [GuildOnly]
    public async Task CreateCommand(
            [Summary("name", "Write a tag name")] string name,
            [Summary("content", "Write message content")] string? messageContent = null,
            [Summary("attachment1", "Add a video or photo")] IAttachment? firstAttachment = null,
            [Summary("attachment2", "Add a video or photo")] IAttachment? secondAttachment = null,
            [Summary("attachment3", "Add a video or photo")] IAttachment? thirdAttachment = null,
            [Summary("hide", "Only the result of the message to you")] bool? hide = false
        )
    {
        var attachments = new[] {firstAttachment, secondAttachment, thirdAttachment};
        var tagContent = new TagContentDto(messageContent, attachments);
        await Context.SendResponse(
            interactiveService,
            await tagsCommand.CreateTagAsync((long)Context.User.Id, (long)Context.Guild.Id, name, tagContent),
            hide ?? false
        );
    }
    
    [SlashCommand("update", "Update a tag")]
    [GuildOnly]
    public async Task UpdateCommand(
        [Summary("name", "Write the name of the tag you want to update")] [Autocomplete(typeof(SearchTagsWhenModifyAutoComplete))] string name,
        // TODO: make below possible, though it requires saving separation of content and attachments as it's currently saved as a single string
        //[Summary("replace", "Whether to replace everything or only what you change (default: false)")] bool replace = false,
        [Summary("content", "Update message content")] string? messageContent = null,
        [Summary("attachment1", "Update video or photo")] IAttachment? firstAttachment = null,
        [Summary("attachment2", "Update video or photo")] IAttachment? secondAttachment = null,
        [Summary("attachment3", "Update video or photo")] IAttachment? thirdAttachment = null,
        [Summary("hide", "Only the result of the message to you")] bool? hide = false
    )
    {
        var hasMessageEditPerms = Context.Guild.Users.Single(x => x.Id == Context.User.Id).GuildPermissions.ManageMessages;
        var attachments = new[] {firstAttachment, secondAttachment, thirdAttachment};
        var tagContent = new TagContentDto(messageContent, attachments);
        await Context.SendResponse(
            interactiveService,
            await tagsCommand.UpdateTagAsync((long)Context.User.Id, (long)Context.Guild.Id, name, tagContent, hasMessageEditPerms),
            hide ?? false
        );
    }
    
    [SlashCommand("delete", "Delete a tag")]
    [GuildOnly]
    public async Task DeleteCommand(
        [Summary("name", "Write a tag name")] [Autocomplete(typeof(SearchTagsWhenModifyAutoComplete))] string name,
        [Summary("hide", "Only the result of the message to you")] bool? hide = false
    )
    {
        var hasMessageEditPerms = Context.Guild.Users.Single(x => x.Id == Context.User.Id).GuildPermissions.ManageMessages;
        await Context.SendResponse(interactiveService,
            await tagsCommand.DeleteTagAsync((long)Context.User.Id, (long)Context.Guild.Id, name, hasMessageEditPerms),
            hide ?? false);
    }

    [SlashCommand("rename", "Rename a tag")]
    [GuildOnly]
    public async Task RenameCommand(
        [Summary("oldName", "Write the name of the tag you want to rename")] string oldName,
        [Summary("newName", "Write the new name of the tag")] string newName,
        [Summary("hide", "Only the result of the message to you")] bool? hide = false
    )
    {
        var hasMessageEditPerms = Context.Guild.Users.Single(x => x.Id == Context.User.Id).GuildPermissions.ManageMessages;
        await Context.SendResponse(interactiveService,
            await tagsCommand.RenameTagAsync((long)Context.User.Id, (long)Context.Guild.Id, oldName, newName, hasMessageEditPerms),
            hide ?? false);
    }

    [SlashCommand("list", "Get a list of tags (all by default)")]
    [GuildOnly]
    public async Task ListCommand(
        [Summary("top", "List the most used tags")] bool top = false,
        [Summary("byAuthor", "List tags by tags author")] SocketGuildUser? user = null,
        [Summary("hide", "Only the result of the message to you")] bool? hide = false
    ) =>
        await Context.SendResponse(interactiveService,
            await tagsCommand.ListTagsAsync((long)Context.Guild.Id, top, user.AsMaybe()),
            hide ?? false);
    
    [SlashCommand("info", "Get info about a tag")]
    [GuildOnly]
    public async Task InfoCommand(
        [Summary("name", "Write a tag name")] [Autocomplete(typeof(SearchTagsAutoComplete))] string name,
        [Summary("hide", "Only the result of the message to you")] bool? hide = false
    ) =>
        await Context.SendResponse(interactiveService,
            await tagsCommand.GetTagInfoAsync((long)Context.Guild.Id, name),
            hide ?? false);
}