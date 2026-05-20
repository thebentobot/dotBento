using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using NetCord.Services.ComponentInteractions;
using dotBento.Bot.Enums;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Resources;
using dotBento.Bot.Utilities;
using dotBento.Domain.Enums;
using dotBento.Domain.Extensions;
using Serilog;

namespace dotBento.Bot.Extensions;

public static class InteractionContextExtensions
{
    public static void LogCommandUsed(this ApplicationCommandContext context, CommandResponse commandResponse = CommandResponse.Ok)
    {
        string? commandName = null;
        if (context.Interaction is SlashCommandInteraction slashCommand)
        {
            commandName = slashCommand.Data.Name;
        }

        Log.Information("SlashCommandUsed: {DiscordUserName} / {DiscordUserId} | {GuildName} / {GuildId} | {CommandResponse} | {MessageContent}",
            context.User?.Username, context.User?.Id, context.Guild?.Name, context.Guild?.Id, commandResponse, commandName);

    }

    public static async Task HandleCommandException(this ApplicationCommandContext context, Exception exception, string? message = null, bool sendReply = true, bool deferFirst = false)
    {
        var referenceId = StringUtilities.GenerateRandomCode();

        var commandName = context.Interaction switch
        {
            SlashCommandInteraction slashCommand => slashCommand.Data.Name,
            UserCommandInteraction userCommand => userCommand.Data.Name,
            _ => "ButtonInteraction"
        };

        Log.Error(exception, "SlashCommandUsed: Error {ReferenceId} | {DiscordUserName} / {DiscordUserId} | {GuildName} / {GuildId} | {CommandResponse} ({Message}) | {MessageContent}",
            referenceId, context.User?.Username, context.User?.Id, context.Guild?.Name, context.Guild?.Id, CommandResponse.Error, message, commandName);

        if (sendReply)
        {
            if (deferFirst)
            {
                await context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));
            }

            await context.Interaction.SendFollowupMessageAsync(new InteractionMessageProperties()
                .WithContent($"Sorry, something went wrong while trying to process `{commandName}`. Please try again later.\n" +
                             $"*Reference id: `{referenceId}`*")
                .WithFlags(MessageFlags.Ephemeral));
        }

    }

    public static async Task SendResponse(this ApplicationCommandContext context, InteractiveService interactiveService, ResponseModel response, bool ephemeral = false)
    {
        var flags = ephemeral ? MessageFlags.Ephemeral : (MessageFlags?)null;

        switch (response.ResponseType)
        {
            case ResponseType.Text:
                await context.Interaction.SendResponseAsync(InteractionCallback.Message(
                    new InteractionMessageProperties()
                        .WithContent(response.Text)
                        .WithAllowedMentions(AllowedMentionsProperties.None)
                        .WithFlags(flags)
                        .WithComponents(response.Components)));
                break;
            case ResponseType.Embed:
                await context.Interaction.SendResponseAsync(InteractionCallback.Message(
                    new InteractionMessageProperties()
                        .WithEmbeds([response.Embed])
                        .WithFlags(flags)
                        .WithComponents(response.Components)));
                break;
            case ResponseType.Paginator:
                _ = interactiveService.SendPaginatorAsync(
                    response.ComponentPaginator ?? throw new InvalidOperationException(),
                    context.Interaction,
                    TimeSpan.FromSeconds(DiscordConstants.PaginationTimeoutInSeconds),
                    InteractionCallbackType.Message,
                    ephemeral: ephemeral);
                break;
            case ResponseType.ImageWithEmbed:
                var imageWithEmbedStream = response.Stream ?? throw new InvalidOperationException("Stream required for ImageWithEmbed");
                var imageWithEmbedFilename = response.FileName ?? throw new InvalidOperationException("FileName required for ImageWithEmbed");
                await context.Interaction.SendResponseAsync(InteractionCallback.Message(
                    new InteractionMessageProperties()
                        .AddAttachments(new AttachmentProperties(
                            (response.Spoiler ? "SPOILER_" : "") + imageWithEmbedFilename,
                            imageWithEmbedStream))
                        .WithEmbeds([response.Embed])
                        .WithFlags(flags)
                        .WithComponents(response.Components)));
                await imageWithEmbedStream.DisposeAsync();
                break;
            case ResponseType.ImageOnly:
                var imageOnlyStream = response.Stream ?? throw new InvalidOperationException("Stream required for ImageOnly");
                var imageOnlyFilename = response.FileName ?? throw new InvalidOperationException("FileName required for ImageOnly");
                await context.Interaction.SendResponseAsync(InteractionCallback.Message(
                    new InteractionMessageProperties()
                        .AddAttachments(new AttachmentProperties(
                            (response.Spoiler ? "SPOILER_" : "") + imageOnlyFilename,
                            imageOnlyStream))
                        .WithFlags(flags)));
                await imageOnlyStream.DisposeAsync();
                break;
            case ResponseType.FileWithEmbed:
                var fileWithEmbedStream = response.Stream ?? throw new InvalidOperationException("Stream required for FileWithEmbed");
                var fileWithEmbedFilename = response.FileName ?? throw new InvalidOperationException("FileName required for FileWithEmbed");
                await context.Interaction.SendResponseAsync(InteractionCallback.Message(
                    new InteractionMessageProperties()
                        .AddAttachments(new AttachmentProperties(
                            (response.Spoiler ? "SPOILER_" : "") + fileWithEmbedFilename,
                            fileWithEmbedStream))
                        .WithEmbeds([response.Embed])
                        .WithFlags(flags)
                        .WithComponents(response.Components)));
                await fileWithEmbedStream.DisposeAsync();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <summary>Sends a deferred "thinking…" response, optionally ephemeral.</summary>
    public static Task DeferResponseAsync(this ApplicationCommandContext context, bool ephemeral = false) =>
        context.Interaction.SendResponseAsync(
            InteractionCallback.DeferredMessage(ephemeral ? MessageFlags.Ephemeral : null));

    public static async Task SendFollowUpResponse(this ApplicationCommandContext context, InteractiveService interactiveService, ResponseModel response, bool ephemeral = false)
    {
        var flags = ephemeral ? MessageFlags.Ephemeral : (MessageFlags?)null;

        switch (response.ResponseType)
        {
            case ResponseType.Text:
                await context.Interaction.SendFollowupMessageAsync(new InteractionMessageProperties()
                    .WithContent(response.Text)
                    .WithAllowedMentions(AllowedMentionsProperties.None)
                    .WithFlags(flags)
                    .WithComponents(response.Components));
                break;
            case ResponseType.Embed:
                await context.Interaction.SendFollowupMessageAsync(new InteractionMessageProperties()
                    .WithEmbeds([response.Embed])
                    .WithFlags(flags)
                    .WithComponents(response.Components));
                break;
            case ResponseType.Paginator:
                await interactiveService.SendPaginatorAsync(
                    response.ComponentPaginator ?? throw new InvalidOperationException(),
                    context.Interaction,
                    response.PaginatorTimeout ?? TimeSpan.FromSeconds(DiscordConstants.PaginationTimeoutInSeconds),
                    InteractionCallbackType.DeferredMessage,
                    ephemeral: ephemeral);
                break;
            case ResponseType.ImageWithEmbed:
                var followupImageEmbedStream = response.Stream ?? throw new InvalidOperationException("Stream required for ImageWithEmbed");
                var followupImageEmbedFilename = (response.FileName ?? throw new InvalidOperationException("FileName required for ImageWithEmbed")).ReplaceInvalidChars().TruncateLongString(60);
                await context.Interaction.SendFollowupMessageAsync(new InteractionMessageProperties()
                    .AddAttachments(new AttachmentProperties(
                        (response.Spoiler ? "SPOILER_" : "") + followupImageEmbedFilename,
                        followupImageEmbedStream))
                    .WithEmbeds([response.Embed])
                    .WithFlags(flags)
                    .WithComponents(response.Components));
                await followupImageEmbedStream.DisposeAsync();
                break;
            case ResponseType.ImageOnly:
                var followupImageOnlyStream = response.Stream ?? throw new InvalidOperationException("Stream required for ImageOnly");
                var followupImageOnlyFilename = response.FileName ?? throw new InvalidOperationException("FileName required for ImageOnly");
                await context.Interaction.SendFollowupMessageAsync(new InteractionMessageProperties()
                    .AddAttachments(new AttachmentProperties(
                        (response.Spoiler ? "SPOILER_" : "") + followupImageOnlyFilename,
                        followupImageOnlyStream))
                    .WithFlags(flags));
                await followupImageOnlyStream.DisposeAsync();
                break;
            case ResponseType.FileWithEmbed:
                var followupFileEmbedStream = response.Stream ?? throw new InvalidOperationException("Stream required for FileWithEmbed");
                var followupFileEmbedFilename = response.FileName ?? throw new InvalidOperationException("FileName required for FileWithEmbed");
                await context.Interaction.SendFollowupMessageAsync(new InteractionMessageProperties()
                    .AddAttachments(new AttachmentProperties(
                        (response.Spoiler ? "SPOILER_" : "") + followupFileEmbedFilename,
                        followupFileEmbedStream))
                    .WithEmbeds([response.Embed])
                    .WithFlags(flags)
                    .WithComponents(response.Components));
                await followupFileEmbedStream.DisposeAsync();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

}

public static class ComponentInteractionContextExtensions
{
    /// <summary>
    /// Modifies the original component message in-place using the embed and components from
    /// <paramref name="response"/>. Use this as the standard way to update a settings panel.
    /// </summary>
    public static Task UpdateResponseAsync(this ComponentInteractionContext context, ResponseModel response) =>
        context.Interaction.SendResponseAsync(InteractionCallback.ModifyMessage(m =>
        {
            m.Embeds = [response.Embed];
            m.Components = response.Components;
        }));

    /// <summary>Sends a short ephemeral text reply to a component interaction.</summary>
    public static Task EphemeralResponseAsync(this ComponentInteractionContext context, string content) =>
        context.Interaction.SendResponseAsync(InteractionCallback.Message(
            new InteractionMessageProperties()
                .WithContent(content)
                .WithFlags(MessageFlags.Ephemeral)));
}
