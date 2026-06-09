using System.Text.RegularExpressions;

namespace dotBento.Infrastructure.Utilities;

public static class ProfileValidationUtilities
{
    public static bool IsValidHttpUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        return uri.Scheme is "http" or "https";
    }

    public static string? NormalizeHex(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return null;
        hex = hex.Trim();
        if (hex.StartsWith('#')) hex = hex[1..];
        if (hex.Length is not 6) return null;
        if (!Regex.IsMatch(hex, "^[0-9A-Fa-f]{6}$")) return null;
        return "#" + hex.ToUpperInvariant();
    }

    public static bool TryValidateTimezone(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(id);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool TryParseBirthday(string input, out string yyyyMmDd)
    {
        // Only accept month and day, normalize to a fixed year (2000) for storage
        yyyyMmDd = string.Empty;
        if (string.IsNullOrWhiteSpace(input)) return false;
        var s = input.Trim();
        // Accept formats: MM-DD, M-D, MM/DD, M/D, and also allow with leading/trailing zeros/spaces
        var sep = s.Contains('/') ? '/' : '-';
        var parts = s.Split(sep, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2) return false;
        if (!int.TryParse(parts[0], out var month) || !int.TryParse(parts[1], out var day)) return false;
        if (month is < 1 or > 12) return false;
        // Use year 2000 (leap year) so Feb 29 is valid
        var year = 2000;
        try
        {
            var dt = new DateTime(year, month, day);
            yyyyMmDd = dt.ToString("yyyy-MM-dd");
            return true;
        }
        catch
        {
            return false;
        }
    }
}