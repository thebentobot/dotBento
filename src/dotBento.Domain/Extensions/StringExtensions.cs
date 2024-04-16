using System.Text.RegularExpressions;

namespace dotBento.Domain.Extensions;

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
        var pattern = new Regex("(@everyone|@here|<@|`|)");
        return pattern.Replace(str, "");
    }
    
    public static bool ContainsSensitiveCharacters(this string str)
    {
        return SensitiveCharacters.Any(str.Contains);
    }

    public static bool ContainsUnicodeCharacter(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        const int maxAnsiCode = 255;

        return input.Any(c => c > maxAnsiCode);
    }

    public static string ReplaceInvalidChars(this string filename)
    {
        filename = filename.Replace("\"", "_");
        filename = filename.Replace("'", "_");
        filename = filename.Replace(".", "_");
        filename = filename.Replace(" ", "_");

        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Join("_", filename.Split(invalidChars));
    }

    private static readonly string[] SensitiveCharacters =
    [
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
        "@"
    ];

    public static string Sanitize(this string text)
    {
        return SensitiveCharacters.Aggregate(text, (current, sensitiveCharacter) => 
            current.Replace(sensitiveCharacter, "\\" + sensitiveCharacter));
    }

    public static string TruncateLongString(this string str, int maxLength)
    {
        return string.IsNullOrEmpty(str) ? str : str[..Math.Min(str.Length, maxLength)];
    }
    
    public static string TrimToMaxLength(this string source, int maxLength)
    {
        if(string.IsNullOrEmpty(source) || source.Length <= maxLength)
            return source;
        
        return string.Concat(source.AsSpan(0, maxLength - 3), "...");
    }

    public static string CapitalizeFirstLetter(this string? str)
    {
        if (String.IsNullOrEmpty(str))
            return String.Empty;
    
        return Char.ToUpper(str[0]) + str.Substring(1);
    }
    
    public static void ReplaceOrAddToList(this List<string> currentList, IEnumerable<string> optionsToAdd)
    {
        foreach (var optionToAdd in optionsToAdd)
        {
            var existingOption = currentList.FirstOrDefault(f => f.ToLower() == optionToAdd.ToLower());

            if (existingOption != null && existingOption != optionToAdd)
            {
                var index = currentList.IndexOf(existingOption);
                if (index != -1)
                {
                    currentList[index] = optionToAdd;
                }
            }
            else if (existingOption == null)
            {
                currentList.Add(optionToAdd);
            }
        }
    }
}