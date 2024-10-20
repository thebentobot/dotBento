using System.Text;
using CSharpFunctionalExtensions;
using dotBento.Domain.Entities.LastFm;
using dotBento.Infrastructure.Extensions;
using dotBento.Infrastructure.Services.Api;
using Ganss.Xss;

namespace dotBento.Infrastructure.Commands;

public class LastFmCommands(
    LastFmApiService lastFmApiService,
    HtmlSanitizer htmlSanitizer,
    SushiiImageServerService sushiiImageServerService)
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
        return valueUser.HasNoValue
            ? Result.Failure<BentoLastFmUserInfo>("No user info found")
            : valueUser.Value.ToBentoLastFmUserInfo();
    }

    public async Task<Result<Stream>> GetLastFmCollageImage(string amountOfImages,
        List<BentoLastFmCollage> collageEntities, string imageServerHost)
    {
        var dims = amountOfImages.Split("x");
        var dimension = Math.Max(1, Math.Min(10, (int)Math.Sqrt(int.Parse(dims[0]) * int.Parse(dims[1]))));
        var itemCount = dimension * dimension;

        var collection = collageEntities
            .Take(itemCount)
            .ToList();

        while (Math.Sqrt(collection.Count) <= dimension - 1) dimension--;

        var screenWidth = Math.Min(collection.Count, dimension) * 300;
        var screenHeight = (int)Math.Ceiling((double)collection.Count / dimension) * 300;

        var htmlString = GenerateCollageHtml(collection, dimension);

        // TODO: Sanitize HTML. Currently disabled due to it sanitizing too well
        //var sanitizedHtml = htmlSanitizer.Sanitize(htmlString);

        var image = await sushiiImageServerService.GetSushiiImage(imageServerHost, htmlString, screenWidth,
            screenHeight);

        return image.IsSuccess
            ? Result.Success(image.Value)
            : Result.Failure<Stream>("Could not get image from Sushii Image Server");
    }

    private static string GenerateCollageHtml(List<BentoLastFmCollage> collection, int dimension)
    {
        var htmlString = new StringBuilder();
        const string css = """
                           
                                   div {
                                       font-size: 0px;
                                       overflow: hidden;
                                       text-overflow: ellipsis;
                                       white-space: nowrap;
                                   }
                                   
                                   body {
                                       display: block;
                                       margin: 0px;
                                   }
                                   
                                   .grid {
                                       background-color: black;
                                   }
                                   
                                   .container {
                                       width: 300px;
                                       display: inline-block;
                                       position: relative;
                                   }
                                   
                                   .text {
                                       width: 296px;
                                       position: absolute;
                                       text-align: left;
                                       line-height: 1;
                                       
                                       font-family: 'Roboto Mono', 'Noto Sans', monospace, sans-serif;
                                       font-size: 16px;
                                       color: white;
                                       text-shadow: 1px 1px black;
                                       top: 2px;
                                       left: 2px;
                                   }
                               
                           """;

        htmlString.Append("<div class=\"grid\">\n");

        var itemIndex = 0;
        for (var i = 0; i < dimension; i++)
        {
            htmlString.Append("<div class=\"row\">\n");

            for (var j = 0; j < dimension; j++)
            {
                if (itemIndex >= collection.Count) break;

                var item = collection[itemIndex];

                htmlString.Append($@"
                <div class=""container"">
                    <img src=""{item.ImageUrl}"" width=""300"" height=""300"">
                    <div class=""text"">{item.Artist}<br>{(item.Name != null ? $"{item.Name}<br>" : "")}Plays: {item.PlayCount}</div>
                </div>\n");

                itemIndex++;
            }

            htmlString.Append("</div>\n");

            if (itemIndex >= collection.Count) break;
        }

        htmlString.Append("</div>");

        return $"""
                
                        <html>
                            <head><meta charset="UTF-8"></head>
                            <style>{css}</style>
                            <body>{htmlString}</body>
                        </html>
                    
                """;
    }
}