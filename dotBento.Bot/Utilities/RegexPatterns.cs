using System.Text.RegularExpressions;

namespace dotBento.Bot.Utilities;

public static class RegexPatterns
{
    public static readonly Regex HasEmoteRegex = new(@"<a?:.+:\d+>", RegexOptions.Compiled | RegexOptions.Multiline);
    public static readonly Regex EmoteRegex = new(@"<:.+:(\d+)>", RegexOptions.Compiled | RegexOptions.Multiline);
    public static readonly Regex AnimatedEmoteRegex = new(@"<a:.+:(\d+)>", RegexOptions.Compiled | RegexOptions.Multiline);
}