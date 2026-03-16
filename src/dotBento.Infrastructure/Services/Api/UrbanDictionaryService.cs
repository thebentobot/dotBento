using System.Text.Json;
using dotBento.Infrastructure.Models.UrbanDictionary;

namespace dotBento.Infrastructure.Services.Api;

public sealed class UrbanDictionaryService(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public async Task<UrbanDictionaryResponse?> GetDefinition(string term)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.urbandictionary.com/v0/define?term={term}");
        using var response = await httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();
        var responseModel = JsonSerializer.Deserialize<UrbanDictionaryResponse>(responseContent, JsonOptions);
        return responseModel;
    }
}
