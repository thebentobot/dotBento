using Fergun.Interactive;
using NetCord;
using NetCord.Rest;
using NetCord.Services.Commands;
using dotBento.Bot.Enums;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Resources;
using dotBento.Bot.Utilities;
using dotBento.Domain.Enums;
using Serilog;

namespace dotBento.Bot.Extensions;

public static class CommandContextExtensions
{
    public static void LogCommandUsed(this CommandContext context, CommandResponse commandResponse = CommandResponse.Ok)
    {
        Log.Information("CommandUsed: {DiscordUserName} / {DiscordUserId} | {GuildName} / {GuildId} | {CommandResponse} | {MessageContent}",
            context.User?.Username, context.User?.Id, context.Guild?.Name, context.Guild?.Id, commandResponse, context.Message.Content);

    }

    public static async Task HandleCommandException(this CommandContext context, Exception exception, string? message = null, bool sendReply = true)
    {
        var referenceId = StringUtilities.GenerateRandomCode();

        Log.Error(exception, "CommandUsed: Error {ReferenceId} | {DiscordUserName} / {DiscordUserId} | {GuildName} / {GuildId} | {CommandResponse} ({Message}) | {MessageContent}",
            referenceId, context.User?.Username, context.User?.Id, context.Guild?.Name, context.Guild?.Id, CommandResponse.Error, message, context.Message.Content);

        if (sendReply)
        {
            if (exception.Message.Contains("error 50013"))
            {
                await context.Client.Rest.SendMessageAsync(context.Message.ChannelId, new MessageProperties()
                    .WithContent("Sorry, something went wrong because the bot is missing permissions. Make sure the bot has `Embed links` and `Attach Files`.\n" +
                                 $"*Reference id: `{referenceId}`*")
                    .WithAllowedMentions(AllowedMentionsProperties.None));
            }
            else
            {
                await context.Client.Rest.SendMessageAsync(context.Message.ChannelId, new MessageProperties()
                    .WithContent("Sorry, something went wrong. Please try again later.\n" +
                                 $"*Reference id: `{referenceId}`*")
                    .WithAllowedMentions(AllowedMentionsProperties.None));
            }
        }

    }

    public static async Task SendResponse(this CommandContext context, InteractiveService interactiveService, ResponseModel response)
    {
        switch (response.ResponseType)
        {
            case ResponseType.Text:
                await context.Client.Rest.SendMessageAsync(context.Message.ChannelId, new MessageProperties()
                    .WithContent(response.Text)
                    .WithAllowedMentions(AllowedMentionsProperties.None)
                    .WithComponents(response.Components));
                break;
            case ResponseType.Embed:
                await context.Client.Rest.SendMessageAsync(context.Message.ChannelId, new MessageProperties()
                    .AddEmbeds(response.Embed)
                    .WithComponents(response.Components));
                break;
            case ResponseType.Paginator:
                _ = interactiveService.SendPaginatorAsync(
                    response.StaticPaginator ?? throw new InvalidOperationException(),
                    context.Channel,
                    TimeSpan.FromSeconds(DiscordConstants.PaginationTimeoutInSeconds));
                break;
            case ResponseType.ImageWithEmbed:
                var imageEmbedFilename = response.FileName;
                await context.Client.Rest.SendMessageAsync(context.Message.ChannelId, new MessageProperties()
                    .AddAttachments(new AttachmentProperties(
                        (response.Spoiler ? "SPOILER_" : "") + imageEmbedFilename,
                        response.Stream))
                    .AddEmbeds(response.Embed)
                    .WithComponents(response.Components));
                if (response.Stream != null) await response.Stream.DisposeAsync();
                break;
            case ResponseType.ImageOnly:
                var imageFilename = response.FileName;
                await context.Client.Rest.SendMessageAsync(context.Message.ChannelId, new MessageProperties()
                    .AddAttachments(new AttachmentProperties(
                        (response.Spoiler ? "SPOILER_" : "") + imageFilename + ".png",
                        response.Stream)));
                if (response.Stream != null) await response.Stream.DisposeAsync();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
