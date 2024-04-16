using Discord;
using Discord.Interactions;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

[Group("tag", "Tags for the server")]
public class TagSlashCommand(InteractiveService interactiveService, TagCommand userCommand)
    : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("create", "Create a tag")]
    public async Task CreateCommand(
            [Summary("name", "Write a tag name")] string name,
            [Summary("content", "Write message content")] string content,
            [Summary("attachments", "Add a video or photo")] IAttachment attachment,
            [Summary("hide", "Only the result of the message to you")] bool? hide = false
        )
    {
        // okay so we now know that you can only import one attachment at a time
        // so we need to decide on a limit and then add it to the end of the content
        // though preferably it would be better to somehow sanitise but reliably keep discord urls even for text content
        // we can make some thorough tests for this with different sorts of discord urls and ways they're altered
        // though we could've just said fk it, this will also allow urls like gifs etc.
        // this needs to happen inside the tag command in the infrastructure
        await Context.SendResponse(interactiveService,
            await userCommand.CreateTagAsync(Context.User.Id, Context.Guild.Id, name, content, attachments),
            hide ?? false);
    }
}