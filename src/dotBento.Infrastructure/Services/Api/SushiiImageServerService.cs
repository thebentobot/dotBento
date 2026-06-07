using System.Text;
using System.Text.Json;
using CSharpFunctionalExtensions;

namespace dotBento.Infrastructure.Services.Api;

public sealed class SushiiImageServerService(HttpClient httpClient)
{
    public async Task<Result<Stream>> GetSushiiImage(string imageServerUrl, string htmlContent, int width, int height, string type = "png")
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, imageServerUrl);
        var requestBody = new
        {
            html = htmlContent,
            width = width.ToString(),
            height = height.ToString(),
            imageFormat = type,
            quality = 100
        };

        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        using var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            return Result.Failure<Stream>("Could not get image from Sushii Image Server");

        var bytes = await response.Content.ReadAsByteArrayAsync();
        return Result.Success<Stream>(new MemoryStream(bytes));
    }
}
