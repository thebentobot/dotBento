using System.Text.RegularExpressions;

namespace dotBento.Bot.Utilities;

public static class RegexPatterns
{
    public static readonly Regex HasEmoteRegex = new Regex(@"<a?:.+:\d+>", RegexOptions.Compiled | RegexOptions.Multiline);
    public static readonly Regex EmoteRegex = new Regex(@"<:.+:(\d+)>", RegexOptions.Compiled | RegexOptions.Multiline);
    public static readonly Regex AnimatedEmoteRegex = new Regex(@"<a:.+:(\d+)>", RegexOptions.Compiled | RegexOptions.Multiline);
}