using CSharpFunctionalExtensions;
using dotBento.Bot.Models;
using dotBento.Bot.Resources;
using dotBento.Domain;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Http;

namespace dotBento.Bot.Services;

public sealed class SpotifyApiService(HttpClient httpClient, IMemoryCache cache, IOptions<BotEnvConfig> options)
{
    public async Task<Result<FullArtist>> GetArtist(string artistName)
    {
        var spotify = GetSpotifyWebApi();

        var result = await cache.GetOrCreateAsync(artistName, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);
            return await spotify.Search.Item(new SearchRequest(SearchRequest.Types.Artist, artistName));
        });
        
        if (result == null || result.Artists.Items?.Any() != true)
        {
            Statistics.SpotifyApiErrors.WithLabels("GetArtist").Inc();
            return Result.Failure<FullArtist>("No artist found");
        }
        
        Statistics.SpotifyApiCalls.WithLabels(nameof(GetArtist)).Inc();
        return Result.Success(result.Artists.Items.First());
    }
    
    public async Task<Result<FullTrack>> GetTrack(string trackName, string artistName)
    {
        var spotify = GetSpotifyWebApi();

        var result = await cache.GetOrCreateAsync($"{trackName} artist:{artistName}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);
            return await spotify.Search.Item(new SearchRequest(SearchRequest.Types.Track, $"{trackName} artist:{artistName}"));
        });
        
        if (result == null || result.Tracks.Items?.Any() != true)
        {
            Statistics.SpotifyApiErrors.WithLabels("GetTrack").Inc();
            return Result.Failure<FullTrack>("No track found");
        }
        
        Statistics.SpotifyApiCalls.WithLabels(nameof(GetTrack)).Inc();
        return Result.Success(result.Tracks.Items.First());
    }
    
    private SpotifyClient GetSpotifyWebApi()
    {
        InitApiClientConfig();

        return new SpotifyClient(DiscordConstants.SpotifyConfig!);
    }

    private void InitApiClientConfig()
    {
        DiscordConstants.SpotifyConfig ??= SpotifyClientConfig
            .CreateDefault()
            .WithHTTPClient(new NetHttpClient(httpClient))
            .WithAuthenticator(new ClientCredentialsAuthenticator(options.Value.Spotify.Key,
                options.Value.Spotify.Secret));
    }
}