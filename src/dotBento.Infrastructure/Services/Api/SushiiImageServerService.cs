using System.Text;
using System.Text.Json;
using CSharpFunctionalExtensions;

namespace dotBento.Infrastructure.Services.Api;

public sealed class SushiiImageServerService(HttpClient httpClient)
{
    public async Task<Result<Stream>> GetSushiiImage(string imageServerHost, string htmlContent, int width, int height, string type = "png")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"http://{imageServerHost}:3000/html");
        var requestBody = new
        {
            html = htmlContent,
            width = width.ToString(),
            height = height.ToString(),
            imageFormat = type,
            quality = 100
        };

        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await httpClient.SendAsync(request);
        
        return !response.IsSuccessStatusCode ? Result.Failure<Stream>("Could not get image from Sushii Image Server") : Result.Success(await response.Content.ReadAsStreamAsync());
    }
}