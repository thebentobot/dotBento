using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CSharpFunctionalExtensions;
using dotBento.Infrastructure.Models.BentoMedia;

namespace dotBento.Infrastructure.Services.Api;

public sealed class BentoMediaServerService(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<Result<MediaResolveResponse>> ResolveAsync(
        string baseUrl,
        string url,
        string? apiKey = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/resolve");
        if (!string.IsNullOrEmpty(apiKey))
            request.Headers.Add("X-API-Key", apiKey);

        request.Content = new StringContent(
            JsonSerializer.Serialize(new { url }),
            Encoding.UTF8,
            "application/json");

        try
        {
            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return Result.Failure<MediaResolveResponse>(
                    $"Media server returned {(int)response.StatusCode}: {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<MediaResolveResponse>(JsonOptions);
            return result is null
                ? Result.Failure<MediaResolveResponse>("Empty response from media server")
                : Result.Success(result);
        }
        catch (Exception ex)
        {
            return Result.Failure<MediaResolveResponse>($"Failed to reach media server: {ex.Message}");
        }
    }

    public async Task<Result<Stream>> ProxyAsync(
        string baseUrl,
        string mediaUrl,
        string? apiKey = null)
    {
        var encodedUrl = Uri.EscapeDataString(mediaUrl);
        var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/proxy?url={encodedUrl}");
        if (!string.IsNullOrEmpty(apiKey))
            request.Headers.Add("X-API-Key", apiKey);

        try
        {
            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return Result.Failure<Stream>($"Proxy returned HTTP {(int)response.StatusCode}");
            var bytes = await response.Content.ReadAsByteArrayAsync();
            return Result.Success<Stream>(new MemoryStream(bytes));
        }
        catch (Exception ex)
        {
            return Result.Failure<Stream>($"Proxy request failed: {ex.Message}");
        }
    }
}
