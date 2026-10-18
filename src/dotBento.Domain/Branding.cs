namespace dotBento.Domain;

/// <summary>Deployment identity shared by Discord presentation and profile rendering.</summary>
public sealed class Branding
{
    public string Name { get; set; } = "Bento";
    public string Description { get; set; } = "A Discord bot with media and entertainment commands.";
    public string Emoji { get; set; } = "🍱";
    public string GiftName { get; set; } = "Bento Box";
    public string GiftPluralName { get; set; } = "Bento Boxes";
    public string GiftEmoji { get; set; } = "🍱";
    public string WebsiteUrl { get; set; } = "https://bentobot.xyz";
    // Null inherits the existing root/path; an explicit empty string disables the page.
    public string? FeaturesUrl { get; set; }
    public string? LeaderboardUrl { get; set; }
    public string? TroubleshootingUrl { get; set; }
    public string? ServerSettingsGuideUrl { get; set; }
    public string DocumentationUrl { get; set; } = "https://docs.bentobot.xyz";
    public string SupportUrl { get; set; } = "https://discord.gg/dd68WwP";
    public string SourceUrl { get; set; } = "https://github.com/thebentobot/bento";
    public string PatreonUrl { get; set; } = "https://www.patreon.com/bentobot";
    public string KoFiUrl { get; set; } = "https://ko-fi.com/bentobot";
    public string VoteUrl { get; set; } = "https://top.gg/bot/787041583580184609/vote";
    public string GiftCommandName { get; set; } = "bento";
    public string AboutCommandName { get; set; } = "bento";
    public string GiftLeaderboardName { get; set; } = "bento";
    public string DisplayName => $"{Name} {Emoji}".Trim();
    public string Gift(int count) => count == 1 ? GiftName : GiftPluralName;
    public string DefaultProfileDescription => $"I am a happy user of {DisplayName}😄";

    public string GetFeaturesUrl() => FeaturesUrl ?? JoinUrl(WebsiteUrl, "features");
    public string GetLeaderboardUrl(long? guildId = null)
    {
        var root = LeaderboardUrl ?? JoinUrl(WebsiteUrl, "leaderboard");
        return guildId.HasValue ? JoinUrl(root, guildId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)) : root;
    }
    public string GetTroubleshootingUrl() => TroubleshootingUrl ?? JoinUrl(DocumentationUrl, "troubleshooting/");
    public string GetServerSettingsGuideUrl() => ServerSettingsGuideUrl ?? JoinUrl(DocumentationUrl, "guides/server-settings/");

    public static string JoinUrl(string root, string path) =>
        string.IsNullOrWhiteSpace(root) ? string.Empty : $"{root.TrimEnd('/')}/{path.TrimStart('/')}";

    public string CommandPath(string canonicalId) => canonicalId switch
    {
        "bento" => GiftCommandName,
        "about:bento" => $"about:{AboutCommandName}",
        "leaderboard:bento:server" => $"leaderboard:{GiftLeaderboardName}:server",
        "leaderboard:bento:global" => $"leaderboard:{GiftLeaderboardName}:global",
        _ => canonicalId
    };
}
