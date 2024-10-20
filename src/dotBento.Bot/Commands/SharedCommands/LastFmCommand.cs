using System.Text;
using Discord;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Resources;
using dotBento.Bot.Services;
using dotBento.Domain.Entities.LastFm;
using dotBento.Infrastructure.Commands;
using dotBento.Infrastructure.Services;
using Fergun.Interactive;
using Humanizer;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Commands.SharedCommands;

public class LastFmCommand(
    LastFmService lastFmService,
    LastFmCommands lastFmCommands,
    IOptions<BotEnvConfig> config,
    SpotifyApiService spotifyApiService)
{
    public async Task<ResponseModel> GetNowPlaying(long userId, string discordName, string discordAvatarUrl)
    {
        var embed = new ResponseModel { ResponseType = ResponseType.Embed };
        var lastFmUser = await lastFmService.GetLastFmAsync(userId);
        if (lastFmUser.HasNoValue)
        {
            embed.Embed
                .WithColor(Color.Red)
                .WithTitle("Error: No Last.fm username saved")
                .WithDescription("Please save your Last.fm username with either the slash command or the text command");
            return embed;
        }

        var response = await lastFmCommands.NowPlaying(lastFmUser.Value.Lastfm1, config.Value.LastFmApiKey);
        if (response.IsFailure)
        {
            embed.Embed
                .WithColor(Color.Red)
                .WithTitle("Error: " + response.Error);
            return embed;
        }

        var bentoLastFmRecentTracks = response.Value.RecentTracks;
        var description = bentoLastFmRecentTracks.Select(recentTrack =>
            $"{(recentTrack.NowPlaying ? "Now Playing" : $"<t:{recentTrack.Date?.ToUnixTimeSeconds()}:R>")}\n**{recentTrack.Artist}** - [{recentTrack.Track}]({recentTrack.Url})\nFrom the album **{recentTrack.Album}**");
        var singleDescription = string.Join("\n\n", description);
        var footer = new EmbedFooterBuilder
        {
            Text = $"Total Tracks: {response.Value.TotalTracks} | Powered by Last.fm",
            IconUrl = "https://www.last.fm/static/images/lastfm_avatar_twitter.52a5d69a85ac.png"
        };
        var author = new EmbedAuthorBuilder
        {
            Name = discordName,
            IconUrl = discordAvatarUrl,
            Url = $"https://www.last.fm/user/{lastFmUser.Value.Lastfm1}"
        };
        embed.Embed
            .WithAuthor(author)
            .WithFooter(footer)
            .WithThumbnailUrl(bentoLastFmRecentTracks.First().Image)
            .WithColor(DiscordConstants.LastFmColorRed)
            .WithDescription(singleDescription);
        return embed;
    }

    public async Task<ResponseModel> GetTopArtists(long userId, string discordName, string discordAvatarUrl,
        string period)
    {
        var embed = new ResponseModel
        {
            ResponseType = ResponseType.Paginator,
        };

        var lastFmUser = await lastFmService.GetLastFmAsync(userId);

        if (lastFmUser.HasNoValue)
        {
            embed.Embed
                .WithColor(Color.Red)
                .WithTitle("Error: No Last.fm username saved")
                .WithDescription("Please save your Last.fm username with either the slash command or the text command");

            embed.ResponseType = ResponseType.Embed;
            return embed;
        }

        var topArtistsResponse =
            await lastFmCommands.GetTopArtists(lastFmUser.Value.Lastfm1, config.Value.LastFmApiKey, period);

        if (topArtistsResponse.IsFailure)
        {
            embed.Embed
                .WithTitle("Error: " + topArtistsResponse.Error)
                .WithColor(Color.Red);

            embed.ResponseType = ResponseType.Embed;
            return embed;
        }

        var topArtists = topArtistsResponse.Value;

        var artistPageChunks = topArtists.ChunkBy(10);

        var pages = new List<PageBuilder>();

        foreach (var artistChunk in artistPageChunks)
        {
            var firstArtistName = artistChunk.First().Name;
            var artistResult = await spotifyApiService.GetArtist(firstArtistName);
            var thumbnailUrl = artistResult.IsSuccess && artistResult.Value.Images.Any()
                ? artistResult.Value.Images.First().Url
                : discordAvatarUrl;

            var pageDescription = string.Join('\n', artistChunk.Select(artist =>
                $"**{artist.Rank}.** [{artist.Name}]({artist.Url}) - {artist.PlayCount} plays"));

            var author = new EmbedAuthorBuilder
            {
                Name = discordName,
                IconUrl = discordAvatarUrl,
                Url = $"https://www.last.fm/user/{lastFmUser.Value.Lastfm1}"
            };

            var footer = new EmbedFooterBuilder
            {
                Text = $"Time period: {period} | Powered by Last.fm",
                IconUrl = "https://www.last.fm/static/images/lastfm_avatar_twitter.52a5d69a85ac.png"
            };

            var page = new PageBuilder()
                .WithAuthor(author)
                .WithFooter(footer)
                .WithDescription(pageDescription)
                .WithColor(DiscordConstants.LastFmColorRed);

            if (!string.IsNullOrEmpty(thumbnailUrl))
            {
                page.WithThumbnailUrl(thumbnailUrl);
            }

            pages.Add(page);
        }

        embed.StaticPaginator = pages.BuildSimpleStaticPaginator();
        embed.ResponseType = ResponseType.Paginator;

        return embed;
    }

    public async Task<ResponseModel> GetTopAlbums(long userId, string discordName, string discordAvatarUrl,
        string period)
    {
        var embed = new ResponseModel
        {
            ResponseType = ResponseType.Paginator,
        };

        var lastFmUser = await lastFmService.GetLastFmAsync(userId);

        if (lastFmUser.HasNoValue)
        {
            embed.Embed
                .WithColor(Color.Red)
                .WithTitle("Error: No Last.fm username saved")
                .WithDescription("Please save your Last.fm username with either the slash command or the text command");

            embed.ResponseType = ResponseType.Embed;
            return embed;
        }

        var topAlbumsResponse =
            await lastFmCommands.GetTopAlbums(lastFmUser.Value.Lastfm1, config.Value.LastFmApiKey, period);

        if (topAlbumsResponse.IsFailure)
        {
            embed.Embed
                .WithTitle("Error: " + topAlbumsResponse.Error)
                .WithColor(Color.Red);

            embed.ResponseType = ResponseType.Embed;
            return embed;
        }

        var topAlbums = topAlbumsResponse.Value;

        var albumsPageChunks = topAlbums.ChunkBy(10);

        var pages = new List<PageBuilder>();

        foreach (var albumChunk in albumsPageChunks)
        {
            var firstArtist = albumChunk.First();
            var thumbnailUrl = firstArtist.ImageUrl ?? discordAvatarUrl;

            var pageDescription = string.Join('\n', albumChunk.Select(album =>
                $"**{album.Rank}.** [{album.Name}]({album.Url}) by **{album.Artist}** - {album.PlayCount} plays"));

            var author = new EmbedAuthorBuilder
            {
                Name = discordName,
                IconUrl = discordAvatarUrl,
                Url = $"https://www.last.fm/user/{lastFmUser.Value.Lastfm1}"
            };

            var footer = new EmbedFooterBuilder
            {
                Text = $"Time period: {period} | Powered by Last.fm",
                IconUrl = "https://www.last.fm/static/images/lastfm_avatar_twitter.52a5d69a85ac.png"
            };

            var page = new PageBuilder()
                .WithAuthor(author)
                .WithFooter(footer)
                .WithDescription(pageDescription)
                .WithColor(DiscordConstants.LastFmColorRed);

            if (!string.IsNullOrEmpty(thumbnailUrl))
            {
                page.WithThumbnailUrl(thumbnailUrl);
            }

            pages.Add(page);
        }

        embed.StaticPaginator = pages.BuildSimpleStaticPaginator();
        embed.ResponseType = ResponseType.Paginator;

        return embed;
    }

    public async Task<ResponseModel> GetTopTracks(long userId, string discordName, string discordAvatarUrl,
        string period)
    {
        var embed = new ResponseModel
        {
            ResponseType = ResponseType.Paginator,
        };

        var lastFmUser = await lastFmService.GetLastFmAsync(userId);

        if (lastFmUser.HasNoValue)
        {
            embed.Embed
                .WithColor(Color.Red)
                .WithTitle("Error: No Last.fm username saved")
                .WithDescription("Please save your Last.fm username with either the slash command or the text command");

            embed.ResponseType = ResponseType.Embed;
            return embed;
        }

        var topTracksResponse =
            await lastFmCommands.GetTopTracks(lastFmUser.Value.Lastfm1, config.Value.LastFmApiKey, period);

        if (topTracksResponse.IsFailure)
        {
            embed.Embed
                .WithTitle("Error: " + topTracksResponse.Error)
                .WithColor(Color.Red);

            embed.ResponseType = ResponseType.Embed;
            return embed;
        }

        var topTracks = topTracksResponse.Value;

        var tracksPageChunks = topTracks.ChunkBy(10);

        var pages = new List<PageBuilder>();

        foreach (var trackChunk in tracksPageChunks)
        {
            var firstArtistName = trackChunk.First().Artist;
            var artistResult = await spotifyApiService.GetArtist(firstArtistName);
            var thumbnailUrl = artistResult.IsSuccess && artistResult.Value.Images.Any()
                ? artistResult.Value.Images.First().Url
                : discordAvatarUrl;

            var pageDescription = string.Join('\n', trackChunk.Select(track =>
                $"**{track.Rank}.** [{track.Name}]({track.Url}) by {track.Artist} - {track.PlayCount} plays"));

            var author = new EmbedAuthorBuilder
            {
                Name = discordName,
                IconUrl = discordAvatarUrl,
                Url = $"https://www.last.fm/user/{lastFmUser.Value.Lastfm1}"
            };

            var footer = new EmbedFooterBuilder
            {
                Text = $"Time period: {period} | Powered by Last.fm",
                IconUrl = "https://www.last.fm/static/images/lastfm_avatar_twitter.52a5d69a85ac.png"
            };

            var page = new PageBuilder()
                .WithAuthor(author)
                .WithFooter(footer)
                .WithDescription(pageDescription)
                .WithColor(DiscordConstants.LastFmColorRed);

            if (!string.IsNullOrEmpty(thumbnailUrl))
            {
                page.WithThumbnailUrl(thumbnailUrl);
            }

            pages.Add(page);
        }

        embed.StaticPaginator = pages.BuildSimpleStaticPaginator();
        embed.ResponseType = ResponseType.Paginator;

        return embed;
    }

    public async Task<ResponseModel> GetUserInfo(long userId, string discordName, string discordAvatarUrl)
    {
        var embed = new ResponseModel
        {
            ResponseType = ResponseType.Embed,
        };

        var lastFmUser = await lastFmService.GetLastFmAsync(userId);

        if (lastFmUser.HasNoValue)
        {
            embed.Embed
                .WithColor(Color.Red)
                .WithTitle("Error: No Last.fm username saved")
                .WithDescription("Please save your Last.fm username with either the slash command or the text command");

            embed.ResponseType = ResponseType.Embed;
            return embed;
        }

        var userInfoResponse = await lastFmCommands.GetUserInfo(lastFmUser.Value.Lastfm1, config.Value.LastFmApiKey);

        if (userInfoResponse.IsFailure)
        {
            embed.Embed
                .WithTitle("Error: " + userInfoResponse.Error)
                .WithColor(Color.Red);

            embed.ResponseType = ResponseType.Embed;
            return embed;
        }

        var userInfo = userInfoResponse.Value;

        var description = new StringBuilder();
        description.AppendLine($"**Name:** {userInfo.Name}");
        description.AppendLine($"**Country:** {userInfo.Country}");
        description.AppendLine($"**Total tracks:** {userInfo.PlayCount}");
        description.AppendLine($"**Registered:** <t:{userInfo.RegisteredAt.ToUnixTimeSeconds()}:R>");

        var author = new EmbedAuthorBuilder
        {
            Name = discordName,
            IconUrl = discordAvatarUrl,
            Url = $"https://www.last.fm/user/{lastFmUser.Value.Lastfm1}"
        };

        var footer = new EmbedFooterBuilder
        {
            Text = $"Powered by Last.fm",
            IconUrl = "https://www.last.fm/static/images/lastfm_avatar_twitter.52a5d69a85ac.png"
        };

        embed.Embed
            .WithAuthor(author)
            .WithFooter(footer)
            .WithDescription(description.ToString())
            .WithThumbnailUrl(userInfo.ImageUrl ?? null)
            .WithColor(DiscordConstants.LastFmColorRed);

        return embed;
    }

    public async Task<ResponseModel> GetRecentTracks(long userId, string username, string userAvatar)
    {
        var embed = new ResponseModel
        {
            ResponseType = ResponseType.Paginator,
        };

        var lastFmUser = await lastFmService.GetLastFmAsync(userId);

        if (lastFmUser.HasNoValue)
        {
            embed.Embed
                .WithColor(Color.Red)
                .WithTitle("Error: No Last.fm username saved")
                .WithDescription("Please save your Last.fm username with either the slash command or the text command");

            embed.ResponseType = ResponseType.Embed;
            return embed;
        }

        var recentTracksResponse =
            await lastFmCommands.GetRecentTracks(lastFmUser.Value.Lastfm1, config.Value.LastFmApiKey);

        if (recentTracksResponse.IsFailure)
        {
            embed.Embed
                .WithTitle("Error: " + recentTracksResponse.Error)
                .WithColor(Color.Red);

            embed.ResponseType = ResponseType.Embed;
            return embed;
        }

        var recentTracks = recentTracksResponse.Value;
        var fromDate = recentTracks.Last().Date.Humanize();
        var toDate = recentTracks.First().NowPlaying ? "Now" : recentTracks.First().Date.Humanize();

        var recentTracksChunks = recentTracks.ChunkBy(10);

        var pages = new List<PageBuilder>();

        foreach (var recentTrackChunk in recentTracksChunks)
        {
            var firstArtist = recentTrackChunk.First();
            var thumbnailUrl = firstArtist.Image;

            var pageDescription = string.Join('\n', recentTrackChunk.Select(recentTrack =>
                $"{(recentTrack.NowPlaying ? "Now Playing" : $"<t:{recentTrack.Date?.ToUnixTimeSeconds()}:R>")} | **{recentTrack.Artist}** - [{recentTrack.Track}]({recentTrack.Url})"));

            var author = new EmbedAuthorBuilder
            {
                Name = username,
                IconUrl = userAvatar,
                Url = $"https://www.last.fm/user/{lastFmUser.Value.Lastfm1}"
            };

            var footer = new EmbedFooterBuilder
            {
                Text = $"From {fromDate} to {toDate} | Powered by Last.fm",
                IconUrl = "https://www.last.fm/static/images/lastfm_avatar_twitter.52a5d69a85ac.png"
            };

            var page = new PageBuilder()
                .WithAuthor(author)
                .WithFooter(footer)
                .WithDescription(pageDescription)
                .WithColor(DiscordConstants.LastFmColorRed);

            if (!string.IsNullOrEmpty(thumbnailUrl))
            {
                page.WithThumbnailUrl(thumbnailUrl);
            }

            pages.Add(page);
        }

        embed.StaticPaginator = pages.BuildSimpleStaticPaginator();
        embed.ResponseType = ResponseType.Paginator;

        return embed;
    }

    public async Task<ResponseModel> SaveLastFmUser(long userId, string username)
    {
        var embed = new ResponseModel
        {
            ResponseType = ResponseType.Embed,
        };

        await lastFmService.SaveLastFmAsync(userId, username);

        var author = new EmbedAuthorBuilder
        {
            Name = "Last.fm",
            IconUrl = "https://www.last.fm/static/images/lastfm_avatar_twitter.52a5d69a85ac.png"
        };

        embed.Embed
            .WithAuthor(author)
            .WithTitle($"You have successfully saved your Last.fm username \"{username}\"")
            .WithColor(DiscordConstants.LastFmColorRed);

        return embed;
    }

    public async Task<ResponseModel> DeleteLastFmUser(long userId)
    {
        var embed = new ResponseModel
        {
            ResponseType = ResponseType.Embed,
        };

        await lastFmService.DeleteLastFmAsync(userId);

        var author = new EmbedAuthorBuilder
        {
            Name = "Last.fm",
            IconUrl = "https://www.last.fm/static/images/lastfm_avatar_twitter.52a5d69a85ac.png"
        };

        embed.Embed
            .WithAuthor(author)
            .WithTitle($"You have successfully deleted your Last.fm username")
            .WithColor(DiscordConstants.LastFmColorRed);

        return embed;
    }

    public async Task<ResponseModel> GetTopArtistsCollage(
        long userId,
        string discordAvatarUrl,
        string period,
        string amountOfImages
    )
    {
        var result = new ResponseModel
        {
            ResponseType = ResponseType.ImageOnly,
        };

        var lastFmUser = await lastFmService.GetLastFmAsync(userId);

        if (lastFmUser.HasNoValue)
        {
            result.ResponseType = ResponseType.Embed;
            result.Embed
                .WithColor(Color.Red)
                .WithTitle("Error: No Last.fm username saved")
                .WithDescription("Please save your Last.fm username with either the slash command or the text command");
            return result;
        }

        var topArtistsResponse = await lastFmCommands.GetTopArtists(lastFmUser.Value.Lastfm1,
            config.Value.LastFmApiKey,
            period);

        if (topArtistsResponse.IsFailure)
        {
            result.Embed
                .WithTitle("Error: " + topArtistsResponse.Error)
                .WithColor(Color.Red);

            result.ResponseType = ResponseType.Embed;
            return result;
        }

        var topArtists = topArtistsResponse.Value;
        var topArtistsWithImages = new List<BentoLastFmTopArtist>();

        foreach (var artist in topArtists)
        {
            var spotifyArtist = await spotifyApiService.GetArtist(artist.Name);
            if (spotifyArtist.IsSuccess && spotifyArtist.Value.Images.Count != 0)
            {
                var newArtist = artist with { ImageUrl = spotifyArtist.Value.Images.First().Url };
                topArtistsWithImages.Add(newArtist);
                continue;
            }

            var defaultArtist = artist with { ImageUrl = discordAvatarUrl };
            topArtistsWithImages.Add(defaultArtist);
        }

        var image = await lastFmCommands.GetLastFmCollageImage(amountOfImages,
            topArtistsWithImages
                .Select(x => new BentoLastFmCollage(x.ImageUrl ?? discordAvatarUrl, x.Name, x.PlayCount, null))
                .ToList(),
            config.Value.ImageServer.ImageServerHost);

        if (image.IsFailure)
        {
            result.ResponseType = ResponseType.Embed;
            result.Embed
                .WithTitle("Error: " + image.Error)
                .WithColor(Color.Red);

            return result;
        }

        result.Stream = image.Value;
        result.FileName = "topartists.png";

        return result;
    }

    public async Task<ResponseModel> GetTopAlbumsCollage(
        long userId,
        string discordAvatarUrl,
        string period,
        string amountOfImages
    )
    {
        var result = new ResponseModel
        {
            ResponseType = ResponseType.ImageOnly,
        };

        var lastFmUser = await lastFmService.GetLastFmAsync(userId);

        if (lastFmUser.HasNoValue)
        {
            result.ResponseType = ResponseType.Embed;
            result.Embed
                .WithColor(Color.Red)
                .WithTitle("Error: No Last.fm username saved")
                .WithDescription("Please save your Last.fm username with either the slash command or the text command");
            return result;
        }

        var topAlbumsResponse = await lastFmCommands.GetTopAlbums(lastFmUser.Value.Lastfm1,
            config.Value.LastFmApiKey,
            period);

        if (topAlbumsResponse.IsFailure)
        {
            result.Embed
                .WithTitle("Error: " + topAlbumsResponse.Error)
                .WithColor(Color.Red);

            result.ResponseType = ResponseType.Embed;
            return result;
        }

        var topAlbums = topAlbumsResponse.Value;

        var image = await lastFmCommands.GetLastFmCollageImage(amountOfImages,
            topAlbums.Select(
                    x => new BentoLastFmCollage(x.ImageUrl ?? discordAvatarUrl, x.Name, x.PlayCount, x.Artist))
                .ToList(),
            config.Value.ImageServer.ImageServerHost);

        if (image.IsFailure)
        {
            result.ResponseType = ResponseType.Embed;
            result.Embed
                .WithTitle("Error: " + image.Error)
                .WithColor(Color.Red);

            return result;
        }

        result.Stream = image.Value;
        result.FileName = "topalbums.png";

        return result;
    }

    public async Task<ResponseModel> GetTopTracksCollage(
        long userId,
        string discordAvatarUrl,
        string period,
        string amountOfImages
    )
    {
        var result = new ResponseModel
        {
            ResponseType = ResponseType.ImageOnly,
        };

        var lastFmUser = await lastFmService.GetLastFmAsync(userId);

        if (lastFmUser.HasNoValue)
        {
            result.ResponseType = ResponseType.Embed;
            result.Embed
                .WithColor(Color.Red)
                .WithTitle("Error: No Last.fm username saved")
                .WithDescription("Please save your Last.fm username with either the slash command or the text command");
            return result;
        }

        var topTracksResponse = await lastFmCommands.GetTopTracks(lastFmUser.Value.Lastfm1,
            config.Value.LastFmApiKey,
            period);

        if (topTracksResponse.IsFailure)
        {
            result.Embed
                .WithTitle("Error: " + topTracksResponse.Error)
                .WithColor(Color.Red);

            result.ResponseType = ResponseType.Embed;
            return result;
        }

        var topTracks = topTracksResponse.Value;
        var topTracksWithImages = new List<BentoLastFmTopTrack>();

        foreach (var track in topTracks)
        {
            var spotifyArtist = await spotifyApiService.GetTrack(track.Name, track.Artist);
            if (spotifyArtist.IsSuccess && spotifyArtist.Value.Album.Images.Count != 0)
            {
                var newTrack = track with { ImageUrl = spotifyArtist.Value.Album.Images.First().Url };
                topTracksWithImages.Add(newTrack);
                continue;
            }

            var defaultTrack = track with { ImageUrl = discordAvatarUrl };
            topTracksWithImages.Add(defaultTrack);
        }

        var image = await lastFmCommands.GetLastFmCollageImage(amountOfImages,
            topTracksWithImages.Select(x =>
                    new BentoLastFmCollage(x.ImageUrl ?? discordAvatarUrl, x.Name, x.PlayCount, x.Artist))
                .ToList(),
            config.Value.ImageServer.ImageServerHost);

        if (image.IsFailure)
        {
            result.ResponseType = ResponseType.Embed;
            result.Embed
                .WithTitle("Error: " + image.Error)
                .WithColor(Color.Red);

            return result;
        }

        result.Stream = image.Value;
        result.FileName = "toptracks.png";

        return result;
    }
}