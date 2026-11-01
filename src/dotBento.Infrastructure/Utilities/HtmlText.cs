using System.Globalization;
using System.Text;

namespace dotBento.Infrastructure.Utilities;

/// <summary>
/// HTML and CSS encoding helpers for values interpolated into the profile and
/// Last.fm templates. Every value that can originate from a Discord user or
/// Last.fm must pass through one of these methods.
/// </summary>
public static class HtmlText
{
    /// <summary>Encodes a value for an HTML text node.</summary>
    public static string Encode(string? value) => Escape(value);

    /// <summary>Encodes a value for an HTML attribute value.</summary>
    public static string Attr(string? value) => Escape(value);

    /// <summary>
    /// Percent-encodes the characters that can terminate a CSS <c>url()</c>
    /// string. This prevents a user-supplied background URL from breaking out
    /// of the stylesheet or the surrounding HTML.
    /// </summary>
    public static string CssUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            switch (character)
            {
                case '\'':
                case '"':
                case '(':
                case ')':
                case '\\':
                case '<':
                case '>':
                case ' ':
                case '\t':
                case '\r':
                case '\n':
                    builder.Append('%').Append(((int)character).ToString("X2", CultureInfo.InvariantCulture));
                    break;
                default:
                    builder.Append(character);
                    break;
            }
        }

        return builder.ToString();
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            switch (character)
            {
                case '&':
                    builder.Append("&amp;");
                    break;
                case '<':
                    builder.Append("&lt;");
                    break;
                case '>':
                    builder.Append("&gt;");
                    break;
                case '"':
                    builder.Append("&quot;");
                    break;
                case '\'':
                    builder.Append("&#x27;");
                    break;
                default:
                    builder.Append(character);
                    break;
            }
        }

        return builder.ToString();
    }
}
