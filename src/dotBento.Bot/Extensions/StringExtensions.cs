using System.Text.RegularExpressions;

namespace dotBento.Bot.Extensions;

public static class StringExtensions
{
    public static IEnumerable<string> SplitByMessageLength(this string str)
    {
        var messageLength = 2000;

        for (var index = 0; index < str.Length; index += messageLength)
        {
            yield return str.Substring(index, Math.Min(messageLength, str.Length - index));
        }
    }

    public static string FilterOutMentions(this string str)
    {
        var pattern = new Regex("(@everyone|@here|<@|`|http://|https://)");
        return pattern.Replace(str, "");
    }

    public static bool ContainsUnicodeCharacter(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        const int MaxAnsiCode = 255;

        return input.Any(c => c > MaxAnsiCode);
    }

    public static string ReplaceInvalidChars(string filename)
    {
        filename = filename.Replace("\"", "_");
        filename = filename.Replace("'", "_");
        filename = filename.Replace(".", "_");
        filename = filename.Replace(" ", "_");

        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Join("_", filename.Split(invalidChars));
    }

    private static readonly string[] SensitiveCharacters = {
        "\\",
        "*",
        "_",
        "~",
        "`",
        ":",
        "/",
        ">",
        "|",
        "#",
    };

    public static string Sanitize(string text)
    {
        if (text != null)
        {
            foreach (string sensitiveCharacter in SensitiveCharacters)
            {
                text = text.Replace(sensitiveCharacter, "\\" + sensitiveCharacter);
            }
        }
        return text;
    }

    public static string TruncateLongString(string str, int maxLength)
    {
        if (string.IsNullOrEmpty(str))
        {
            return str;
        }

        return str.Substring(0, Math.Min(str.Length, maxLength));
    }
}