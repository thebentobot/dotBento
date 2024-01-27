using System.Text.Json;
using dotBento.Infrastructure.Models.UrbanDictionary;

namespace dotBento.Infrastructure.Services.Api;

public class UrbanDictionaryService(HttpClient httpClient)
{
    public async Task<UrbanDictionaryResponse?> GetDefinition(string term)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.urbandictionary.com/v0/define?term={term}");
        var response = await httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
        };
        var responseModel = JsonSerializer.Deserialize<UrbanDictionaryResponse>(responseContent, options);
        return responseModel;
    }
}