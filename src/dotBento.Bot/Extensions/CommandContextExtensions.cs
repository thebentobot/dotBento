using Discord;
using Discord.Commands;
using dotBento.Bot.Enums;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Resources;
using dotBento.Bot.Utilities;
using dotBento.Domain;
using dotBento.Domain.Enums;
using Fergun.Interactive;
using Serilog;

namespace dotBento.Bot.Extensions;

public static class CommandContextExtensions
{
    public static void LogCommandUsed(this ICommandContext context, CommandResponse commandResponse = CommandResponse.Ok)
    {
        Log.Information("CommandUsed: {DiscordUserName} / {DiscordUserId} | {GuildName} / {GuildId} | {CommandResponse} | {MessageContent}",
            context.User?.Username, context.User?.Id, context.Guild?.Name, context.Guild?.Id, commandResponse, context.Message.Content);

        PublicProperties.UsedCommandsResponses.TryAdd(context.Message.Id, commandResponse);
    }
    
    public static async Task HandleCommandException(this ICommandContext context, Exception exception, string? message = null, bool sendReply = true)
    {
        var referenceId = StringUtilities.GenerateRandomCode();

        Log.Error(exception, "CommandUsed: Error {ReferenceId} | {DiscordUserName} / {DiscordUserId} | {GuildName} / {GuildId} | {CommandResponse} ({Message}) | {MessageContent}",
            referenceId, context.User?.Username, context.User?.Id, context.Guild?.Name, context.Guild?.Id, CommandResponse.Error, message, context.Message.Content);

        if (sendReply)
        {
            if (exception.Message.Contains("error 50013"))
            {
                await context.Channel.SendMessageAsync("Sorry, something went wrong because the bot is missing permissions. Make sure the bot has `Embed links` and `Attach Files`.\n" +
                                                       $"*Reference id: `{referenceId}`*", allowedMentions: AllowedMentions.None);
            }
            else
            {
                await context.Channel.SendMessageAsync("Sorry, something went wrong. Please try again later.\n" +
                                                       $"*Reference id: `{referenceId}`*", allowedMentions: AllowedMentions.None);
            }
        }

        PublicProperties.UsedCommandsErrorReferences.TryAdd(context.Message.Id, referenceId);
    }

    public static async Task SendResponse(this ICommandContext context, InteractiveService interactiveService, ResponseModel response)
    {
        switch (response.ResponseType)
        {
            case ResponseType.Text:
                await context.Channel.SendMessageAsync(response.Text, allowedMentions: AllowedMentions.None, components: response.Components?.Build());
                break;
            case ResponseType.Embed:
                await context.Channel.SendMessageAsync("", false, response.Embed.Build(), components: response.Components?.Build());
                break;
            case ResponseType.Paginator:
                _ = interactiveService.SendPaginatorAsync(
                    response.StaticPaginator ?? throw new InvalidOperationException(),
                    context.Channel,
                    TimeSpan.FromMinutes(DiscordConstants.PaginationTimeoutInSeconds));
                break;
            case ResponseType.ImageWithEmbed:
                var imageEmbedFilename = response.FileName;
                await context.Channel.SendFileAsync(
                    response.Stream,
                    imageEmbedFilename,
                    null,
                    false,
                    response.Embed.Build(),
                    isSpoiler: response.Spoiler,
                    components: response.Components?.Build());
                if (response.Stream != null) await response.Stream.DisposeAsync();
                break;
            case ResponseType.ImageOnly:
                var imageFilename = response.FileName;
                await context.Channel.SendFileAsync(
                    response.Stream,
                    imageFilename + ".png",
                    isSpoiler: response.Spoiler);
                if (response.Stream != null) await response.Stream.DisposeAsync();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}