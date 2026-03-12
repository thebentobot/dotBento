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
using dotBento.Domain;
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

        PublicProperties.UsedCommandsResponses.TryAdd(context.Interaction.Id, commandResponse);
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

        PublicProperties.UsedCommandsErrorReferences.TryAdd(context.Interaction.Id, referenceId);
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
                    response.StaticPaginator ?? throw new InvalidOperationException(),
                    context.Interaction,
                    TimeSpan.FromSeconds(DiscordConstants.PaginationTimeoutInSeconds),
                    ephemeral: ephemeral);
                break;
            case ResponseType.ImageWithEmbed:
                await context.Interaction.SendResponseAsync(InteractionCallback.Message(
                    new InteractionMessageProperties()
                        .AddAttachments(new AttachmentProperties(
                            (response.Spoiler ? "SPOILER_" : "") + response.FileName,
                            response.Stream))
                        .WithEmbeds([response.Embed])
                        .WithFlags(flags)
                        .WithComponents(response.Components)));
                break;
            case ResponseType.ImageOnly:
                await context.Interaction.SendResponseAsync(InteractionCallback.Message(
                    new InteractionMessageProperties()
                        .AddAttachments(new AttachmentProperties(
                            (response.Spoiler ? "SPOILER_" : "") + response.FileName,
                            response.Stream))
                        .WithFlags(flags)));
                await response.Stream!.DisposeAsync();
                break;
            case ResponseType.FileWithEmbed:
                await context.Interaction.SendResponseAsync(InteractionCallback.Message(
                    new InteractionMessageProperties()
                        .AddAttachments(new AttachmentProperties(
                            (response.Spoiler ? "SPOILER_" : "") + response.FileName,
                            response.Stream))
                        .WithEmbeds([response.Embed])
                        .WithFlags(flags)
                        .WithComponents(response.Components)));
                if (response.Stream != null) await response.Stream.DisposeAsync();
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
                    response.StaticPaginator ?? throw new InvalidOperationException(),
                    context.Interaction,
                    response.PaginatorTimeout ?? TimeSpan.FromSeconds(DiscordConstants.PaginationTimeoutInSeconds),
                    InteractionCallbackType.DeferredMessage,
                    ephemeral: ephemeral);
                break;
            case ResponseType.ImageWithEmbed:
                var imageEmbedFilename = (response.FileName ?? throw new InvalidOperationException()).ReplaceInvalidChars().TruncateLongString(60);
                await context.Interaction.SendFollowupMessageAsync(new InteractionMessageProperties()
                    .AddAttachments(new AttachmentProperties(
                        (response.Spoiler ? "SPOILER_" : "") + imageEmbedFilename + ".png",
                        response.Stream))
                    .WithEmbeds([response.Embed])
                    .WithFlags(flags)
                    .WithComponents(response.Components));
                if (response.Stream != null) await response.Stream.DisposeAsync();
                break;
            case ResponseType.ImageOnly:
                await context.Interaction.SendFollowupMessageAsync(new InteractionMessageProperties()
                    .AddAttachments(new AttachmentProperties(
                        (response.Spoiler ? "SPOILER_" : "") + response.FileName,
                        response.Stream))
                    .WithFlags(flags));
                await response.Stream!.DisposeAsync();
                break;
            case ResponseType.FileWithEmbed:
                var fileEmbedFilename = response.FileName ?? throw new InvalidOperationException();
                await context.Interaction.SendFollowupMessageAsync(new InteractionMessageProperties()
                    .AddAttachments(new AttachmentProperties(
                        (response.Spoiler ? "SPOILER_" : "") + fileEmbedFilename,
                        response.Stream))
                    .WithEmbeds([response.Embed])
                    .WithFlags(flags)
                    .WithComponents(response.Components));
                if (response.Stream != null) await response.Stream.DisposeAsync();
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
