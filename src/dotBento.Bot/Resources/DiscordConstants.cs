using Discord;
using SpotifyAPI.Web;

namespace dotBento.Bot.Resources;

public sealed class DiscordConstants
{
    public static readonly Color LastFmColorRed = new(186, 0, 0);
    public static readonly Color SpotifyColorGreen = new(30, 215, 97);

    public const int PaginationTimeoutInSeconds = 120;

    public static SpotifyClientConfig? SpotifyConfig = null;
}
