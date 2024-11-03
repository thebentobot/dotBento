using System.Text.Json;
using CSharpFunctionalExtensions;
using dotBento.Infrastructure.Models.LastFm;
using dotBento.Infrastructure.Models.LastFm.RecentTracks;
using dotBento.Infrastructure.Models.LastFm.TopAlbums;
using dotBento.Infrastructure.Models.LastFm.TopArtists;
using dotBento.Infrastructure.Models.LastFm.TopTracks;
using dotBento.Infrastructure.Models.LastFm.UserInfo;
using Microsoft.AspNetCore.WebUtilities;

namespace dotBento.Infrastructure.Services.Api;

public sealed class LastFmApiService(HttpClient httpClient)
{
    private const string ApiUrl = "https://ws.audioscrobbler.com/2.0/";
    
    public async Task<Result<RecentTracksResponse>> GetRecentTracks(string lastFmUsername, string apiKey, int? limit = 50)
    {
        var parameters = new Dictionary<string, string?>
        {
            { "user", lastFmUsername },
            { "method", ApiMethod.RecentTracks },
            { "api_key", apiKey },
            { "format", "json" },
            { "limit", limit?.ToString() }
        };

        var response = await httpClient.GetAsync(
            QueryHelpers.AddQueryString(ApiUrl, parameters));

        if (!response.IsSuccessStatusCode)
        {
            return Result.Failure<RecentTracksResponse>(LastFmApiError((int)response.StatusCode, lastFmUsername));
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
        };
        var responseModel = JsonSerializer.Deserialize<RecentTracksResponse>(responseContent, options);

        return responseModel != null ? Result.Success(responseModel) : Result.Failure<RecentTracksResponse>("Could not deserialize the response from lastfm. It might be down.");
    }
    
    public async Task<Result<TopTracksResponse>> GetTopTracks(string lastFmUsername, string apiKey, string period, int? limit = 50)
    {
        var parameters = new Dictionary<string, string?>
        {
            { "user", lastFmUsername },
            { "method", ApiMethod.TopTracks },
            { "api_key", apiKey },
            { "format", "json" },
            { "limit", limit?.ToString() },
            { "period", period }
        };

        var response = await httpClient.GetAsync(
            QueryHelpers.AddQueryString(ApiUrl, parameters));

        if (!response.IsSuccessStatusCode)
        {
            return Result.Failure<TopTracksResponse>(LastFmApiError((int)response.StatusCode, lastFmUsername));
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
        };
        var responseModel = JsonSerializer.Deserialize<TopTracksResponse>(responseContent, options);

        return responseModel != null ? Result.Success(responseModel) : Result.Failure<TopTracksResponse>("Could not deserialize the response from lastfm. It might be down.");
    }
    
    public async Task<Result<TopAlbumsResponse>> GetTopAlbums(string lastFmUsername, string apiKey, string period, int? limit = 50)
    {
        var parameters = new Dictionary<string, string?>
        {
            { "user", lastFmUsername },
            { "method", ApiMethod.TopAlbums },
            { "api_key", apiKey },
            { "format", "json" },
            { "limit", limit?.ToString() },
            { "period", period }
        };

        var response = await httpClient.GetAsync(
            QueryHelpers.AddQueryString(ApiUrl, parameters));

        if (!response.IsSuccessStatusCode)
        {
            return Result.Failure<TopAlbumsResponse>(LastFmApiError((int)response.StatusCode, lastFmUsername));
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
        };
        var responseModel = JsonSerializer.Deserialize<TopAlbumsResponse>(responseContent, options);

        return responseModel != null ? Result.Success(responseModel) : Result.Failure<TopAlbumsResponse>("Could not deserialize the response from lastfm. It might be down.");
    }
    
    public async Task<Result<TopArtistsResponse>> GetTopArtists(string lastFmUsername, string apiKey, string period, int? limit = 50)
    {
        var parameters = new Dictionary<string, string?>
        {
            { "user", lastFmUsername },
            { "method", ApiMethod.TopArtists },
            { "api_key", apiKey },
            { "format", "json" },
            { "limit", limit?.ToString() },
            { "period", period }
        };

        var response = await httpClient.GetAsync(
            QueryHelpers.AddQueryString(ApiUrl, parameters));

        if (!response.IsSuccessStatusCode)
        {
            return Result.Failure<TopArtistsResponse>(LastFmApiError((int)response.StatusCode, lastFmUsername));
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
        };
        var responseModel = JsonSerializer.Deserialize<TopArtistsResponse>(responseContent, options);

        return responseModel != null ? Result.Success(responseModel) : Result.Failure<TopArtistsResponse>("Could not deserialize the response from lastfm. It might be down.");
    }
    
    public async Task<Result<UserInfoResponse>> GetUserInfo(string lastFmUsername, string apiKey)
    {
        var parameters = new Dictionary<string, string?>
        {
            { "user", lastFmUsername },
            { "method", ApiMethod.UserInfo },
            { "api_key", apiKey },
            { "format", "json" }
        };

        var response = await httpClient.GetAsync(
            QueryHelpers.AddQueryString(ApiUrl, parameters));

        if (!response.IsSuccessStatusCode)
        {
            return Result.Failure<UserInfoResponse>(LastFmApiError((int)response.StatusCode, lastFmUsername));
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
        };
        var responseModel = JsonSerializer.Deserialize<UserInfoResponse>(responseContent, options);

        return responseModel != null ? Result.Success(responseModel) : Result.Failure<UserInfoResponse>("Could not deserialize the response from lastfm. It might be down.");
    }

    private static string LastFmApiError(int responseStatusCode, string lastFmUsername) =>
        responseStatusCode switch
        {
            400 => $"Last.fm API returned a 400 Bad Request. This is likely due to an invalid username: {lastFmUsername}",
            403 => $"Last.fm API returned a 403 Forbidden. This is likely due to an invalid API key.",
            404 => $"Last.fm API returned a 404 Not Found. This is likely due to an invalid username: {lastFmUsername}",
            500 => $"Last.fm API returned a 500 Internal Server Error. This is likely due to an internal error on Last.fm's side.",
            503 => $"Last.fm API returned a 503 Service Unavailable. This is likely due to Last.fm's API being down.",
            _ => $"Last.fm API returned a {responseStatusCode} status code. This is likely due to an internal error on Last.fm's side."
        };
}