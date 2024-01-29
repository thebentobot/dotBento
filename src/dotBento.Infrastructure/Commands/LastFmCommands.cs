using CSharpFunctionalExtensions;
using dotBento.Domain.Entities.LastFm;
using dotBento.Infrastructure.Extensions;
using dotBento.Infrastructure.Models.LastFm;
using dotBento.Infrastructure.Services.Api;
using dotBento.Infrastructure.Utilities;

namespace dotBento.Infrastructure.Commands;

public class LastFmCommands(LastFmApiService lastFmApiService)
{
    public async Task<Result<List<BentoLastFmTopArtist>>> GetTopArtists(
        string userName,
        string apiKey,
        string period)
    {
        var response = await lastFmApiService.GetTopArtists(userName, apiKey, period);
    
        if (response.IsFailure)
        {
            return Result.Failure<List<BentoLastFmTopArtist>>(response.Error);
        }
        
        var topArtists = response.Value.TopArtists.AsMaybe();
        
        if (topArtists.HasNoValue)
        {
            return Result.Failure<List<BentoLastFmTopArtist>>("No top artists found");
        }
    
        var topArtistsList = topArtists.Value.Artist
            .Select(x => x.ToBentoLastFmTopArtist())
            .ToList();
    
        return Result.Success(topArtistsList);
    }
    
    public async Task<Result<List<BentoLastFmTopAlbum>>> GetTopAlbums(
        string userName,
        string apiKey,
        string period)
    {
        var response = await lastFmApiService.GetTopAlbums(userName, apiKey, period);
    
        if (response.IsFailure)
        {
            return Result.Failure<List<BentoLastFmTopAlbum>>(response.Error);
        }
        
        var topArtists = response.Value.TopAlbums.AsMaybe();
        
        if (topArtists.HasNoValue)
        {
            return Result.Failure<List<BentoLastFmTopAlbum>>("No top albums found");
        }
    
        var topArtistsList = topArtists.Value.Album
            .Select(x => x.ToBentoLastFmTopAlbum())
            .ToList();
    
        return Result.Success(topArtistsList);
    }
    
    public async Task<Result<List<BentoLastFmTopTrack>>> GetTopTracks(
        string userName,
        string apiKey,
        string period)
    {
        var response = await lastFmApiService.GetTopTracks(userName, apiKey, period);
    
        if (response.IsFailure)
        {
            return Result.Failure<List<BentoLastFmTopTrack>>(response.Error);
        }
        
        var topTracks = response.Value.TopTracks.AsMaybe();
        
        if (topTracks.HasNoValue)
        {
            return Result.Failure<List<BentoLastFmTopTrack>>("No top tracks found");
        }
    
        var topTracksList = topTracks.Value.Track
            .Select(x => x.ToBentoLastFmTopTrack())
            .ToList();
    
        return Result.Success(topTracksList);
    }
    
    public async Task<Result<BentoLastFmRecentTracksWithTotalTracks>> NowPlaying(string userName, string apiKey)
    {
        var response = await lastFmApiService.GetRecentTracks(userName, apiKey, 2);
    
        if (response.IsFailure)
        {
            return Result.Failure<BentoLastFmRecentTracksWithTotalTracks>(response.Error);
        }
        
        var recentTracks = response.Value.RecentTracks.AsMaybe();
        
        return recentTracks.HasNoValue
            ? Result.Failure<BentoLastFmRecentTracksWithTotalTracks>("No recent tracks found")
            : recentTracks.Value.ToBentoLastFmRecentTracksWithTotalTracks();
    }
    
    public async Task<Result<List<BentoLastFmRecentTrack>>> GetRecentTracks(string userName, string apiKey)
    {
        var response = await lastFmApiService.GetRecentTracks(userName, apiKey);
    
        if (response.IsFailure)
        {
            return Result.Failure<List<BentoLastFmRecentTrack>>(response.Error);
        }
        
        var recentTracks = response.Value.RecentTracks.AsMaybe();
        
        return recentTracks.HasNoValue
            ? Result.Failure<List<BentoLastFmRecentTrack>>("No recent tracks found")
            : recentTracks.Value.Track.Select(x => x.ToBentoLastFmRecentTrack()).Take(50).ToList();
    }
    
    public async Task<Result<BentoLastFmUserInfo>> GetUserInfo(string userName, string apiKey)
    {
        var response = await lastFmApiService.GetUserInfo(userName, apiKey);
    
        if (response.IsFailure)
        {
            return Result.Failure<BentoLastFmUserInfo>(response.Error);
        }

        var valueUser = response.Value.User.AsMaybe();
        return valueUser.HasNoValue ? Result.Failure<BentoLastFmUserInfo>("No user info found") : valueUser.Value.ToBentoLastFmUserInfo();
    }
}