using Fergun.Interactive.Pagination;
using NetCord;
using NetCord.Rest;
using SpotifyAPI.Web;

namespace dotBento.Bot.Resources;

public sealed class DiscordConstants
{
    public static Color BentoYellow = new(253, 224, 71);

    public static Color ErrorRed = new(255, 0, 0);

    public static Color LastFmColorRed = new(186, 0, 0);

    public static Color WarningColorOrange = new(255, 174, 66);

    public static Color SuccessColorGreen = new(50, 205, 50);

    public static Color InformationColorBlue = new(68, 138, 255);

    public static Color SpotifyColorGreen = new(30, 215, 97);


    public static readonly List<PaginatorButton> PaginationEmotes =
    [
        new(EmojiProperties.Custom(PagesFirst), PaginatorAction.SkipToStart, ButtonStyle.Secondary),
        new(EmojiProperties.Custom(PagesPrevious), PaginatorAction.Backward, ButtonStyle.Secondary),
        new(EmojiProperties.Custom(PagesNext), PaginatorAction.Forward, ButtonStyle.Secondary),
        new(EmojiProperties.Custom(PagesLast), PaginatorAction.SkipToEnd, ButtonStyle.Secondary)
    ];

    public const ulong PagesFirst = 883825508633182208;
    public const ulong PagesPrevious = 883825508507336704;
    public const ulong PagesNext = 883825508087922739;
    public const ulong PagesLast = 883825508482183258;
    public const ulong PagesGoTo = 1138849626234036264;

    public const string WebsiteUrl = "https://bentobot.xyz";

    public const int PaginationTimeoutInSeconds = 120;

    public const string FiveOrMoreUp = "<:5_or_more_up:912380324841918504>";
    public const string OneToFiveUp = "<:1_to_5_up:912085138232442920>";
    public const string SamePosition = "<:same_position:912374491752046592>";
    public const string OneToFiveDown = "<:1_to_5_down:912085138245029888>";
    public const string FiveOrMoreDown = "<:5_or_more_down:912380324753838140>";
    public const string New = "<:new:912087988001980446>";

    public static SpotifyClientConfig? SpotifyConfig = null;
}
