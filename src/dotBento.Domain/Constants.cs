namespace dotBento.Domain;

// TODO Standardise colours here
public static class Constants
{
    public const ulong BotProductionId = 787041583580184609;

    public const ulong BotDevelopmentId = 1193608563050958948;

    public const string StartPrefix = "?";

    public static readonly IReadOnlyCollection<string> CommandNames = new List<string>
    {
        "About",
        "Avatar",
        "Banner",
        "Bento",
        "Choose",
        "Game",
        "LastFm",
        "Ping",
        "Server",
        "Urban",
        "User",
        "Weather",
        "deleteWeather",
        "saveWeather",
        "EightBall",
        "Member",
        "Roll",
        "Rps",
        "ServerInfo",
        "Profile",
        "Tags",
        "Reminder",
        "Colour",
        "DominantColour",
    };

    public static readonly IReadOnlyCollection<string> AliasNames = new List<string>
    {
        "Pick",
        "Av",
        "Pfp",
        "FM",
        "guildMember",
        "guildInfo",
        "Rank",
        "Tag",
        "Remind",
        "color",
        "colors",
        "colours",
        "hex",
        "rgb",
        "dominantColor",
    };
}