using Discord;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Resources;
using dotBento.Infrastructure.Models.BentoMedia;
using dotBento.Infrastructure.Services.Api;
using Fergun.Interactive;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Commands.SharedCommands;

public sealed class MediaCommand(
    BentoMediaServerService bentoMediaServerService,
    IOptions<BotEnvConfig> config)
{
    public async Task<ResponseModel> GetMediaResponseAsync(string url)
    {
        var fetchResult = await FetchMediaAsync(url);
        if (fetchResult.IsFailure)
            return ErrorResponse(fetchResult.Error);

        var media = fetchResult.Value;
        var attachments = media.Content.Attachments;

        if (attachments.Count == 0)
            return ErrorResponse("No media found in this post.");

        if (attachments.Count == 1)
            return await BuildSingleResponseAsync(media, attachments[0]);

        return BuildPaginatorResponse(media, attachments);
    }

    private async Task<ResponseModel> BuildSingleResponseAsync(
        MediaResolveResponse media,
        MediaAttachment attachment)
    {
        var embed    = BuildBaseEmbed(media);
        var baseUrl  = GetBaseUrl()!;
        var isImage  = attachment.Type == "image";
        var fileName = isImage ? "image.jpg" : "video.mp4";

        var downloadResult = await DownloadAsync(attachment.Url);
        if (downloadResult.IsSuccess)
        {
            return new ResponseModel
            {
                ResponseType = ResponseType.FileWithEmbed,
                Embed        = embed,
                Stream       = downloadResult.Value,
                FileName     = fileName,
            };
        }

        // Download failed — embed URL for images, show URL in description for videos
        if (isImage)
        {
            embed.WithImageUrl(ResolveUrl(attachment, baseUrl));
            return new ResponseModel { ResponseType = ResponseType.Embed, Embed = embed };
        }

        var desc = string.IsNullOrEmpty(media.Content.Caption)
            ? attachment.Url
            : $"{media.Content.Caption}\n\n{attachment.Url}";
        embed.WithDescription(desc);
        return new ResponseModel { ResponseType = ResponseType.Embed, Embed = embed };
    }

    private static ResponseModel BuildPaginatorResponse(
        MediaResolveResponse media,
        IReadOnlyList<MediaAttachment> attachments)
    {
        var pages = attachments.Select((attachment, i) =>
        {
            var page = BuildBaseEmbed(media);
            page.WithFooter(new EmbedFooterBuilder().WithText($"{i + 1} / {attachments.Count}"));

            if (attachment.Type == "image")
                page.WithImageUrl(attachment.Url);
            else
            {
                var currentDesc = page.Description ?? string.Empty;
                page.WithDescription(string.IsNullOrEmpty(currentDesc)
                    ? attachment.Url
                    : $"{currentDesc}\n\n{attachment.Url}");
            }

            return PageBuilder.FromEmbedBuilder(page);
        }).ToList();

        return new ResponseModel
        {
            ResponseType     = ResponseType.Paginator,
            StaticPaginator  = pages.BuildStaticPaginator(),
            PaginatorTimeout = TimeSpan.FromMinutes(5),
        };
    }

    private static EmbedBuilder BuildBaseEmbed(MediaResolveResponse media)
    {
        var (platformLabel, platformColor) = media.Platform switch
        {
            "tiktok"  => ("TikTok",      new Color(0x010101)),
            "twitter" => ("Twitter / X", new Color(0x1DA1F2)),
            _         => (media.Platform, DiscordConstants.BentoYellow),
        };

        var embed = new EmbedBuilder()
            .WithColor(platformColor)
            .WithTitle(platformLabel)
            .WithUrl(media.SourceUrl);

        if (!string.IsNullOrEmpty(media.Author.Username))
        {
            var authorBuilder = new EmbedAuthorBuilder().WithName($"@{media.Author.Username}");
            var profileUrl    = GetAuthorProfileUrl(media);
            if (profileUrl != null)
                authorBuilder.WithUrl(profileUrl);
            embed.WithAuthor(authorBuilder);
        }

        var descParts = new List<string>();
        if (!string.IsNullOrEmpty(media.Content.Caption))
            descParts.Add(media.Content.Caption);
        if (media.PostedAt.HasValue)
        {
            var epoch = new DateTimeOffset(media.PostedAt.Value).ToUnixTimeSeconds();
            descParts.Add($"<t:{epoch}:R>");
        }
        if (descParts.Count > 0)
            embed.WithDescription(string.Join("\n", descParts));

        return embed;
    }

    private static string? GetAuthorProfileUrl(MediaResolveResponse media) =>
        string.IsNullOrEmpty(media.Author.Username) ? null : media.Platform switch
        {
            "twitter" => $"https://twitter.com/{media.Author.Username}",
            "tiktok"  => $"https://www.tiktok.com/@{media.Author.Username}",
            _         => null,
        };

    private static string ResolveUrl(MediaAttachment attachment, string baseUrl) =>
        attachment.Proxy
            ? $"{baseUrl}/proxy?url={Uri.EscapeDataString(attachment.Url)}"
            : attachment.Url;

    private static ResponseModel ErrorResponse(string error)
    {
        var response = new ResponseModel { ResponseType = ResponseType.Embed };
        if (error == "not_configured")
        {
            response.Embed
                .WithColor(Color.Orange)
                .WithTitle("Media commands are not enabled")
                .WithDescription("This bot instance has not been configured with bento-media-server. Contact the bot operator.");
        }
        else
        {
            response.Embed
                .WithColor(Color.Red)
                .WithTitle("Failed to fetch media")
                .WithDescription(error);
        }
        return response;
    }

    private async Task<CSharpFunctionalExtensions.Result<MediaResolveResponse>> FetchMediaAsync(string url)
    {
        var mediaConfig = config.Value.MediaServer;
        if (string.IsNullOrEmpty(mediaConfig.Url))
            return CSharpFunctionalExtensions.Result.Failure<MediaResolveResponse>("not_configured");

        var apiKey = string.IsNullOrEmpty(mediaConfig.ApiKey) ? null : mediaConfig.ApiKey;
        return await bentoMediaServerService.ResolveAsync(mediaConfig.Url, url, apiKey);
    }

    private async Task<CSharpFunctionalExtensions.Result<Stream>> DownloadAsync(string mediaUrl)
    {
        var baseUrl = GetBaseUrl();
        if (baseUrl is null)
            return CSharpFunctionalExtensions.Result.Failure<Stream>("not_configured");
        return await bentoMediaServerService.ProxyAsync(baseUrl, mediaUrl, GetApiKey());
    }

    private string? GetBaseUrl() =>
        string.IsNullOrEmpty(config.Value.MediaServer.Url) ? null : config.Value.MediaServer.Url;

    private string? GetApiKey() =>
        string.IsNullOrEmpty(config.Value.MediaServer.ApiKey) ? null : config.Value.MediaServer.ApiKey;
}
