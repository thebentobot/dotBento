using Discord;
using Discord.WebSocket;
using dotBento.Bot.Enums;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Resources;
using dotBento.Bot.Utilities;
using dotBento.Domain;
using dotBento.Domain.Enums;
using dotBento.Domain.Extensions;
using Fergun.Interactive;
using Serilog;

namespace dotBento.Bot.Extensions;

public static class InteractionContextExtensions
{
    public static void LogCommandUsed(this IInteractionContext context, CommandResponse commandResponse = CommandResponse.Ok)
    {
        string? commandName = null;
        if (context.Interaction is SocketSlashCommand socketSlashCommand)
        {
            commandName = socketSlashCommand.CommandName;
        }

        Log.Information("SlashCommandUsed: {DiscordUserName} / {DiscordUserId} | {GuildName} / {GuildId} | {CommandResponse} | {MessageContent}",
            context.User?.Username, context.User?.Id, context.Guild?.Name, context.Guild?.Id, commandResponse, commandName);

        PublicProperties.UsedCommandsResponses.TryAdd(context.Interaction.Id, commandResponse);
    }

    public static async Task HandleCommandException(this IInteractionContext context, Exception exception, string? message = null, bool sendReply = true, bool deferFirst = false)
    {
        var referenceId = StringUtilities.GenerateRandomCode();

        var commandName = context.Interaction switch
        {
            SocketSlashCommand socketSlashCommand => socketSlashCommand.CommandName,
            SocketUserCommand socketUserCommand => socketUserCommand.CommandName,
            // TODO: Add support for ButtonInteraction and SelectMenuInteraction
            SocketInteraction socketInteraction => "ButtonInteraction",
            _ => null
        };

        Log.Error(exception, "SlashCommandUsed: Error {ReferenceId} | {DiscordUserName} / {DiscordUserId} | {GuildName} / {GuildId} | {CommandResponse} ({Message}) | {MessageContent}",
            referenceId, context.User?.Username, context.User?.Id, context.Guild?.Name, context.Guild?.Id, CommandResponse.Error, message, commandName);

        if (sendReply)
        {
            if (deferFirst)
            {
                await context.Interaction.DeferAsync(ephemeral: true);
            }

            await context.Interaction.FollowupAsync($"Sorry, something went wrong while trying to process `{commandName}`. Please try again later.\n" +
                                                    $"*Reference id: `{referenceId}`*", ephemeral: true);
        }

        PublicProperties.UsedCommandsErrorReferences.TryAdd(context.Interaction.Id, referenceId);
    }

    public static async Task SendResponse(this IInteractionContext context, InteractiveService interactiveService, ResponseModel response, bool ephemeral = false)
    {
        switch (response.ResponseType)
        {
            case ResponseType.Text:
                await context.Interaction.RespondAsync(response.Text, allowedMentions: AllowedMentions.None, ephemeral: ephemeral, components: response.Components?.Build());
                break;
            case ResponseType.Embed:
                await context.Interaction.RespondAsync(null, new[] { response.Embed.Build() }, ephemeral: ephemeral, components: response.Components?.Build());
                break;
            case ResponseType.Paginator:
                _ = interactiveService.SendPaginatorAsync(
                    response.StaticPaginator ?? throw new InvalidOperationException(),
                    (SocketInteraction)context.Interaction,
                    TimeSpan.FromMinutes(DiscordConstants.PaginationTimeoutInSeconds),
                    ephemeral: ephemeral);
                break;
            case ResponseType.ImageWithEmbed:
                var imageEmbedFilename =
                    response.FileName;
                await context.Interaction.RespondWithFileAsync(response.Stream,
                    (response.Spoiler
                        ? "SPOILER_"
                        : "") +
                    imageEmbedFilename,
                    null,
                    [response.Embed?.Build()],
                    ephemeral: ephemeral,
                    components: response.Components?.Build());
                break;
            case ResponseType.ImageOnly:
                var imageName = response.FileName;
                await context.Interaction.RespondWithFileAsync(response.Stream,
                    (response.Spoiler
                        ? "SPOILER_"
                        : "") +
                    imageName,
                    ephemeral: ephemeral);
                await response.Stream.DisposeAsync();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public static async Task SendFollowUpResponse(this IInteractionContext context, InteractiveService interactiveService, ResponseModel response, bool ephemeral = false)
    {
        switch (response.ResponseType)
        {
            case ResponseType.Text:
                await context.Interaction.FollowupAsync(response.Text, allowedMentions: AllowedMentions.None, ephemeral: ephemeral, components: response.Components?.Build());
                break;
            case ResponseType.Embed:
                await context.Interaction.FollowupAsync(null, new[] { response.Embed.Build() }, ephemeral: ephemeral, components: response.Components?.Build());
                break;
            case ResponseType.Paginator:
                _ = interactiveService.SendPaginatorAsync(
                    response.StaticPaginator ?? throw new InvalidOperationException(),
                    (SocketInteraction)context.Interaction,
                    TimeSpan.FromMinutes(DiscordConstants.PaginationTimeoutInSeconds),
                    InteractionResponseType.DeferredChannelMessageWithSource,
                    ephemeral: ephemeral);
                break;
            case ResponseType.ImageWithEmbed:
                var imageEmbedFilename = (response.FileName ?? throw new InvalidOperationException()).ReplaceInvalidChars().TruncateLongString(60);
                await context.Interaction.FollowupWithFileAsync(response.Stream,
                    (response.Spoiler
                        ? "SPOILER_"
                        : "") +
                    imageEmbedFilename +
                    ".png",
                    null,
                    new[] { response.Embed.Build() },
                    ephemeral: ephemeral,
                    components: response.Components?.Build());
                if (response.Stream != null) await response.Stream.DisposeAsync();
                break;
            case ResponseType.ImageOnly:
                var imageName = response.FileName;
                await context.Interaction.FollowupWithFileAsync(response.Stream,
                (response.Spoiler
                    ? "SPOILER_"
                    : "") +
                imageName,
                ephemeral: ephemeral);
                await response.Stream.DisposeAsync();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public static async Task UpdateInteractionEmbed(this IInteractionContext context, ResponseModel response, InteractiveService? interactiveService = null)
    {
        var message = (context.Interaction as SocketMessageComponent)?.Message;

        if (message == null)
        {
            return;
        }

        if (response.ResponseType == ResponseType.Paginator)
        {
            if (interactiveService != null) await ModifyPaginator(interactiveService, message, response);
            return;
        }

        await context.ModifyMessage(message, response);
    }

    public static async Task UpdateMessageEmbed(this IInteractionContext context, ResponseModel response, string messageId)
    {
        var parsedMessageId = ulong.Parse(messageId);
        var msg = await context.Channel.GetMessageAsync(parsedMessageId);

        if (msg is not IUserMessage message)
        {
            return;
        }

        await context.ModifyMessage(message, response);
    }

    private static async Task ModifyMessage(this IInteractionContext context, IUserMessage message, ResponseModel response)
    {
        await message.ModifyAsync(m =>
        {
            if (response.Components != null) m.Components = response.Components.Build();
            m.Embed = response.Embed.Build();
        });

        await context.Interaction.DeferAsync();
    }

    private static async Task ModifyPaginator(InteractiveService interactiveService, IUserMessage message, ResponseModel response) =>
        await interactiveService.SendPaginatorAsync(
            response.StaticPaginator ?? throw new InvalidOperationException(),
            message,
            TimeSpan.FromMinutes(DiscordConstants.PaginationTimeoutInSeconds));
}