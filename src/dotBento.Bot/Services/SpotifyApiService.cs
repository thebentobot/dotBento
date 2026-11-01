using CSharpFunctionalExtensions;
using dotBento.Bot.Models;
using dotBento.Domain;
using Microsoft.Extensions.Options;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Http;

namespace dotBento.Bot.Services;

public sealed class SpotifyApiService
{
    private readonly SpotifyResultCache _cache;
    private readonly Lazy<ISpotifyClient> _spotify;

    public SpotifyApiService(HttpClient httpClient, SpotifyResultCache cache, IOptions<BotEnvConfig> options)
    {
        _cache = cache;
        _spotify = new Lazy<ISpotifyClient>(() => new SpotifyClient(SpotifyClientConfig.CreateDefault()
            .WithHTTPClient(new NetHttpClient(httpClient))
            .WithAuthenticator(new ClientCredentialsAuthenticator(options.Value.Spotify.Key,
                options.Value.Spotify.Secret))));
    }

    internal SpotifyApiService(ISpotifyClient spotify, SpotifyResultCache cache)
    {
        _spotify = new Lazy<ISpotifyClient>(() => spotify);
        _cache = cache;
    }

    public async Task<Result<FullArtist>> GetArtist(string artistName)
    {
        var result = await _cache.GetOrCreateAsync($"artist:{artistName}", async () =>
        {
            var response = await _spotify.Value.Search.Item(new SearchRequest(SearchRequest.Types.Artist, artistName));
            return (response.Artists?.Items?.FirstOrDefault()).AsMaybe();
        });
        
        if (result.HasNoValue)
        {
            Statistics.SpotifyApiErrors.WithLabels("GetArtist").Inc();
            return Result.Failure<FullArtist>("No artist found");
        }
        
        Statistics.SpotifyApiCalls.WithLabels(nameof(GetArtist)).Inc();
        return Result.Success(result.Value);
    }
    
    public async Task<Result<FullTrack>> GetTrack(string trackName, string artistName)
    {
        var result = await _cache.GetOrCreateAsync($"track:{trackName} artist:{artistName}", async () =>
        {
            var response = await _spotify.Value.Search.Item(new SearchRequest(SearchRequest.Types.Track, $"{trackName} artist:{artistName}"));
            return (response.Tracks?.Items?.FirstOrDefault()).AsMaybe();
        });
        
        if (result.HasNoValue)
        {
            Statistics.SpotifyApiErrors.WithLabels("GetTrack").Inc();
            return Result.Failure<FullTrack>("No track found");
        }
        
        Statistics.SpotifyApiCalls.WithLabels(nameof(GetTrack)).Inc();
        return Result.Success(result.Value);
    }
    
}
