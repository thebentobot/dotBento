using System;
using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using dotBento.Bot.Models;

namespace dotBento.Bot.Services
{
    public class UrbanDictionaryService(HttpClient httpClient)
    {
        public async Task<Maybe<UrbanDictionaryResponse>> GetDefinition(string term)
        {
            try
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
                return responseModel.AsMaybe();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}