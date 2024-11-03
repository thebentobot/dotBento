using CSharpFunctionalExtensions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using dotBento.Bot.Attributes;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using dotBento.Bot.Models.Discord;
using dotBento.Infrastructure.Dto.Tags;
using Fergun.Interactive;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Commands.TextCommands;

[Name("Tags")]
public sealed class TagsTextCommand(
    IOptions<BotEnvConfig> botSettings,
    InteractiveService interactiveService,
    TagsCommand tagsCommand) : BaseCommandModule(botSettings)
{
    [Command("tags", RunMode = RunMode.Async)]
    [Summary("Create, get or search, and manage tags. Tags are custom text responses that can be created and used by anyone in the server. It can be used to store information, links, or anything you want to share with others. It can be both text and image attachments.")]
    [Alias("tag")]
    [Examples(
        "tags create <tag name> <tag content>",
        "tags add <tag name> <tag content>",
        "tags get <tag name>",
        "tags search <tag name>",
        "tags delete <tag name>",
        "tags remove <tag name>",
        "tags list <top/author>",
        "tags edit <tag name> <new tag content>",
        "tags update <tag name> <new tag content>",
        "tags info <tag name>",
        "tags rename <tag name> <new tag name>",
        "tags random"
        )]
    [GuildOnly]
    public async Task TagsCommand([Remainder] string? input = null)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            await Context.SendResponse(interactiveService, ErrorEmbed("Please provide an argument to the command. You can check the usage of the command with the `tags help` command."));
            return;
        }
        
        _ = Context.Channel.TriggerTypingAsync();

        var args = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var command = args[0].ToLower();
        var userId = (long)Context.User.Id;
        var guildId = (long)Context.Guild.Id;
        var hasMessageEditPerms = Context.Guild.Users.Single(x => x.Id == Context.User.Id).GuildPermissions.ManageMessages;

        switch (command)
        {
            case "create":
            case "add":
                if (args.Length < 3)
                {
                    await Context.SendResponse(interactiveService, ErrorEmbed("Usage: tags create <tag name> <tag content>. You can check the usage of the command with the `tags help` command."));
                    return;
                }
                var createName = args[1];
                var createContent = string.Join(' ', args.Skip(2));
                var tagContentDto = new TagContentDto(createContent, []);
                await Context.SendResponse(
                    interactiveService,
                    await tagsCommand.CreateTagAsync(userId, guildId, createName, tagContentDto));
                break;

            case "get":
                if (args.Length < 2)
                {
                    await Context.SendResponse(interactiveService, ErrorEmbed("Usage: tags get <tag name>. You can check the usage of the command with the `tags help` command."));
                    return;
                }
                var getName = args[1];
                await Context.SendResponse(
                    interactiveService,
                    await tagsCommand.FindTagAsync(guildId, getName));
                break;

            case "search":
                if (args.Length < 2)
                {
                    await Context.SendResponse(interactiveService, ErrorEmbed("Usage: tags search <tag name>. You can check the usage of the command with the `tags help` command."));
                    return;
                }
                var query = string.Join(' ', args.Skip(1));
                await Context.SendResponse(
                    interactiveService,
                    await tagsCommand.SearchTagsAsync(guildId, query));
                break;

            case "delete":
            case "remove":
                if (args.Length < 2)
                {
                    await Context.SendResponse(interactiveService, ErrorEmbed("Usage: tags delete <tag name>. You can check the usage of the command with the `tags help` command."));
                    return;
                }
                var deleteName = args[1];
                await Context.SendResponse(
                    interactiveService,
                    await tagsCommand.DeleteTagAsync(userId, guildId, deleteName, hasMessageEditPerms));
                break;

            case "list":
                var top = args.Length > 1 && args[1].Equals("top", StringComparison.OrdinalIgnoreCase);
                var author = Maybe<SocketGuildUser>.None;
                if (args.Length > 1 && !top)
                {
                    var mentionedUser = Context.Message.MentionedUsers.FirstOrDefault();
                    if (mentionedUser != null)
                    {
                        var guildUser = Context.Guild.Users.Single(x => x.Id == mentionedUser.Id);
                        author = guildUser;
                    }
                }
                await Context.SendResponse(
                    interactiveService,
                    await tagsCommand.ListTagsAsync(guildId, top, author));
                break;

            case "edit":
            case "update":
                if (args.Length < 3)
                {
                    await Context.SendResponse(interactiveService, ErrorEmbed("Usage: tags update <tag name> <new tag content>. You can check the usage of the command with the `tags help` command."));
                    return;
                }
                var updateName = args[1];
                var updateContent = string.Join(' ', args.Skip(2));
                var updateTagContentDto = new TagContentDto(updateContent, []);
                await Context.SendResponse(
                    interactiveService,
                    await tagsCommand.UpdateTagAsync(userId, guildId, updateName, updateTagContentDto, hasMessageEditPerms));
                break;

            case "info":
                if (args.Length < 2)
                {
                    await Context.SendResponse(interactiveService, ErrorEmbed("Usage: tags info <tag name>. You can check the usage of the command with the `tags help` command."));
                    return;
                }
                var infoName = args[1];
                await Context.SendResponse(
                    interactiveService,
                    await tagsCommand.GetTagInfoAsync(guildId, infoName));
                break;

            case "rename":
                if (args.Length < 3)
                {
                    await Context.SendResponse(interactiveService, ErrorEmbed("Usage: tags rename <tag name> <new tag name>. You can check the usage of the command with the `tags help` command."));
                    return;
                }
                var oldName = args[1];
                var newName = args[2];
                await Context.SendResponse(
                    interactiveService,
                    await tagsCommand.RenameTagAsync(userId, guildId, oldName, newName, hasMessageEditPerms));
                break;

            case "random":
                await Context.SendResponse(
                    interactiveService,
                    await tagsCommand.GetRandomTagAsync(userId, guildId));
                break;

            default:
                var tag = await tagsCommand.MaybeFindTagAsync(guildId, command);
                if (tag.HasValue)
                {
                    await Context.SendResponse(interactiveService, tag.Value);
                    break;
                }
                await Context.SendResponse(interactiveService, ErrorEmbed("Invalid command. Please check the usage of the command with the `tags help` command."));
                break;
        }
    }
    
    private static ResponseModel ErrorEmbed(string error)
    {
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        embed.Embed.WithTitle(error)
            .WithColor(Color.Red);
        return embed;
    }
}