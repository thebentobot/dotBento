namespace dotBento.Domain;

public static class Constants
{
    public const ulong DefaultSupportGuildId = 714496317522444352;

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
        "Timezone",
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
        "tz",
        "time",
    };
}