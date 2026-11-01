using System.Text;
using System.Text.Json;
using CSharpFunctionalExtensions;
using dotBento.Domain;
using dotBento.Infrastructure.Services.Assets;
using Prometheus;

namespace dotBento.Infrastructure.Services.Api;

/// <summary>
/// Client for the internal HTML-to-image renderer. The public API matches the
/// old sushii-image-server call, with an optional asset inliner so generated
/// HTML can be rendered by a service that has no network access.
/// </summary>
public sealed class ImageRendererService(HttpClient httpClient, IHtmlAssetInliner? assetInliner = null)
{
    public async Task<Result<Stream>> GetImage(
        string rendererUrl,
        string htmlContent,
        int width,
        int height,
        string type = "png",
        CancellationToken cancellationToken = default)
    {
        var html = assetInliner is null
            ? htmlContent
            : await assetInliner.InlineAsync(htmlContent, cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Post, rendererUrl);
        var requestBody = new
        {
            html,
            width = width.ToString(),
            height = height.ToString(),
            imageFormat = type,
            quality = 100
        };

        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        using (Statistics.ImageRenderDuration.WithLabels(type).NewTimer())
        {
            response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }

        if (!response.IsSuccessStatusCode)
        {
            response.Dispose();
            return Result.Failure<Stream>("Could not get image from the image renderer");
        }

        try
        {
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return Result.Success<Stream>(new HttpResponseMessageStream(response, stream));
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }
}
