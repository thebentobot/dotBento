namespace dotBento.Domain;

public static class Constants
{
    public const ulong BotProductionId = 714496317522444352;

    public const ulong BotDevelopmentId = 790353119795871744;

    public const string StartPrefix = "_";

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
    };
    
    public static readonly IReadOnlyCollection<string> AliasNames = new List<string>
    {
        "Pick",
        "Av",
        "Pfp",
        "FM",
        "guildMember",
        "guildInfo",
    };
}