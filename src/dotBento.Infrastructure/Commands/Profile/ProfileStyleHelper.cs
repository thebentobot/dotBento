namespace dotBento.Infrastructure.Commands.Profile;

/// <summary>
/// Helper class for profile styling operations like color and opacity conversion
/// </summary>
public static class ProfileStyleHelper
{
    /// <summary>
    /// Converts a color and opacity percentage into a hex color with alpha channel
    /// </summary>
    /// <param name="hexColor">Base hex color (e.g., "#1F2937")</param>
    /// <param name="opacityPercentage">Opacity from 0-100</param>
    /// <returns>Hex color with alpha channel (e.g., "#1F293766")</returns>
    public static string ToColorWithOpacity(string hexColor, int? opacityPercentage)
    {
        var opacityHex = ConvertOpacityToHex(opacityPercentage);
        return $"{hexColor}{opacityHex}";
    }

    /// <summary>
    /// Converts an opacity percentage to a two-character hex string
    /// </summary>
    /// <param name="opacityPercentage">Opacity from 0-100, null defaults to 100 (fully opaque)</param>
    /// <returns>Two-character hex string representing opacity</returns>
    public static string ConvertOpacityToHex(int? opacityPercentage)
    {
        var opacityValue = opacityPercentage ?? 100;
        var hexValue = (int)Math.Round(opacityValue / 100.0 * 255);
        return hexValue.ToString("X2");
    }

    /// <summary>
    /// Determines the appropriate font size for a username based on its length
    /// </summary>
    public static string GetUsernameFontSize(string username) =>
        username.Length switch
        {
            <= 15 => "24px",
            <= 20 => "18px",
            <= 25 => "15px",
            _ => "11px"
        };

    /// <summary>
    /// Determines the appropriate font size for Last.fm text based on its length
    /// </summary>
    public static string GetLastFmTextFontSize(int textLength) =>
        textLength switch
        {
            <= 36 => "16px",
            <= 40 => "14px",
            <= 46 => "12px",
            _ => "10px"
        };

    /// <summary>
    /// Generates an HTML image tag for an emote URL
    /// </summary>
    public static string GetEmoteHtml(string emoteUrl) =>
        $"<img src=\"{emoteUrl}\" width=\"24\" height=\"24\">";
}
