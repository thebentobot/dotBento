using CSharpFunctionalExtensions;
using NetCord;
using NetCord.Gateway;
using NetCord.Services.ApplicationCommands;
using dotBento.Bot.Attributes;
using dotBento.Bot.AutoCompleteHandlers;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using dotBento.Bot.Services;
using dotBento.Infrastructure.Dto.Tags;
using dotBento.Infrastructure.Services;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

[SlashCommand("tag", "Tags for the server")]
public sealed class TagsSlashCommand(InteractiveService interactiveService, TagsCommand tagsCommand, UserSettingService userSettingService, GuildMemberLookupService memberLookup)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    [SubSlashCommand("get", "Get a tag")]
    [GuildOnly]
    public async Task GetCommand(
        [SlashCommandParameter(Name = "name", Description = "Write a tag name", AutocompleteProviderType = typeof(SearchTagsAutoComplete))] string name,
        [SlashCommandParameter(Name = "hide", Description = "Only the result of the message to you")] bool? hide = null
    ) =>
        await Context.SendResponse(
            interactiveService,
            await tagsCommand.FindTagAsync((long)Context.Guild!.Id, name),
            hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id)
        );

    [SubSlashCommand("random", "Get a random tag")]
    [GuildOnly]
    public async Task RandomCommand(
        [SlashCommandParameter(Name = "hide", Description = "Only the result of the message to you")] bool? hide = null
    ) =>
        await Context.SendResponse(
            interactiveService,
            await tagsCommand.GetRandomTagAsync((long)Context.User.Id, (long)Context.Guild!.Id),
            hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id)
        );

    [SubSlashCommand("search", "Search for a tag")]
    [GuildOnly]
    public async Task SearchCommand(
        [SlashCommandParameter(Name = "query", Description = "Search for a tag by name and content")] string query,
        [SlashCommandParameter(Name = "hide", Description = "Only the result of the message to you")] bool? hide = null
    ) =>
        await Context.SendResponse(
            interactiveService,
            await tagsCommand.SearchTagsAsync((long)Context.Guild!.Id, query),
            hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id)
        );

    [SubSlashCommand("create", "Create a tag")]
    [GuildOnly]
    public async Task CreateCommand(
            [SlashCommandParameter(Name = "name", Description = "Write a tag name")] string name,
            [SlashCommandParameter(Name = "content", Description = "Write message content")] string? messageContent = null,
            [SlashCommandParameter(Name = "attachment1", Description = "Add a video or photo")] Attachment? firstAttachment = null,
            [SlashCommandParameter(Name = "attachment2", Description = "Add a video or photo")] Attachment? secondAttachment = null,
            [SlashCommandParameter(Name = "attachment3", Description = "Add a video or photo")] Attachment? thirdAttachment = null,
            [SlashCommandParameter(Name = "hide", Description = "Only the result of the message to you")] bool? hide = null
        )
    {
        var attachments = new[] {firstAttachment, secondAttachment, thirdAttachment};
        var tagContent = new TagContentDto(messageContent, attachments);
        await Context.SendResponse(
            interactiveService,
            await tagsCommand.CreateTagAsync((long)Context.User.Id, (long)Context.Guild!.Id, name, tagContent),
            hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id)
        );
    }

    [SubSlashCommand("update", "Update a tag")]
    [GuildOnly]
    public async Task UpdateCommand(
        [SlashCommandParameter(Name = "name", Description = "Write the name of the tag you want to update", AutocompleteProviderType = typeof(SearchTagsWhenModifyAutoComplete))] string name,
        // TODO: make below possible, though it requires saving separation of content and attachments as it's currently saved as a single string
        //[SlashCommandParameter(Name = "replace", Description = "Whether to replace everything or only what you change (default: false)")] bool replace = false,
        [SlashCommandParameter(Name = "content", Description = "Update message content")] string? messageContent = null,
        [SlashCommandParameter(Name = "attachment1", Description = "Update video or photo")] Attachment? firstAttachment = null,
        [SlashCommandParameter(Name = "attachment2", Description = "Update video or photo")] Attachment? secondAttachment = null,
        [SlashCommandParameter(Name = "attachment3", Description = "Update video or photo")] Attachment? thirdAttachment = null,
        [SlashCommandParameter(Name = "hide", Description = "Only the result of the message to you")] bool? hide = null
    )
    {
        var guildUser = await memberLookup.GetOrFetchAsync(Context.Guild!.Id, Context.User.Id, Context.Guild);
        var hasMessageEditPerms = guildUser != null && guildUser.HasGuildPermission(Context.Guild!, Permissions.ManageMessages);
        var attachments = new[] {firstAttachment, secondAttachment, thirdAttachment};
        var tagContent = new TagContentDto(messageContent, attachments);
        await Context.SendResponse(
            interactiveService,
            await tagsCommand.UpdateTagAsync((long)Context.User.Id, (long)Context.Guild!.Id, name, tagContent, hasMessageEditPerms),
            hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id)
        );
    }

    [SubSlashCommand("delete", "Delete a tag")]
    [GuildOnly]
    public async Task DeleteCommand(
        [SlashCommandParameter(Name = "name", Description = "Write a tag name", AutocompleteProviderType = typeof(SearchTagsWhenModifyAutoComplete))] string name,
        [SlashCommandParameter(Name = "hide", Description = "Only the result of the message to you")] bool? hide = null
    )
    {
        var guildUser = await memberLookup.GetOrFetchAsync(Context.Guild!.Id, Context.User.Id, Context.Guild);
        var hasMessageEditPerms = guildUser != null && guildUser.HasGuildPermission(Context.Guild!, Permissions.ManageMessages);
        await Context.SendResponse(interactiveService,
            await tagsCommand.DeleteTagAsync((long)Context.User.Id, (long)Context.Guild!.Id, name, hasMessageEditPerms),
            hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
    }

    [SubSlashCommand("rename", "Rename a tag")]
    [GuildOnly]
    public async Task RenameCommand(
        [SlashCommandParameter(Name = "old-name", Description = "Write the name of the tag you want to rename")] string oldName,
        [SlashCommandParameter(Name = "new-name", Description = "Write the new name of the tag")] string newName,
        [SlashCommandParameter(Name = "hide", Description = "Only the result of the message to you")] bool? hide = null
    )
    {
        var guildUser = await memberLookup.GetOrFetchAsync(Context.Guild!.Id, Context.User.Id, Context.Guild);
        var hasMessageEditPerms = guildUser != null && guildUser.HasGuildPermission(Context.Guild!, Permissions.ManageMessages);
        await Context.SendResponse(interactiveService,
            await tagsCommand.RenameTagAsync((long)Context.User.Id, (long)Context.Guild!.Id, oldName, newName, hasMessageEditPerms),
            hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
    }

    [SubSlashCommand("list", "Get a list of tags (all by default)")]
    [GuildOnly]
    public async Task ListCommand(
        [SlashCommandParameter(Name = "top", Description = "List the most used tags")] bool top = false,
        [SlashCommandParameter(Name = "by-author", Description = "List tags by tags author")] GuildUser? user = null,
        [SlashCommandParameter(Name = "hide", Description = "Only the result of the message to you")] bool? hide = null
    ) =>
        await Context.SendResponse(interactiveService,
            await tagsCommand.ListTagsAsync((long)Context.Guild!.Id, top, user.AsMaybe()),
            hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));

    [SubSlashCommand("info", "Get info about a tag")]
    [GuildOnly]
    public async Task InfoCommand(
        [SlashCommandParameter(Name = "name", Description = "Write a tag name", AutocompleteProviderType = typeof(SearchTagsAutoComplete))] string name,
        [SlashCommandParameter(Name = "hide", Description = "Only the result of the message to you")] bool? hide = null
    ) =>
        await Context.SendResponse(interactiveService,
            await tagsCommand.GetTagInfoAsync((long)Context.Guild!.Id, name),
            hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
}
