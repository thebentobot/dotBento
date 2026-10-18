using System.Globalization;
using System.Text.RegularExpressions;
using Discord;
using dotBento.Bot.Enums;
using dotBento.Bot.Models;
using dotBento.Bot.Models.Discord;
using dotBento.Domain;
using dotBento.Domain.Enums;
using Fergun.Interactive.Pagination;

namespace dotBento.Bot.Services;

/// <summary>A validated, per-host presentation snapshot. Never changes process-wide defaults.</summary>
public sealed class BotPresentation
{
    public BotEnvConfig Config { get; }
    public Branding Brand => Config.Branding;
    public bool HasWebsite => !string.IsNullOrWhiteSpace(Brand.WebsiteUrl);
    public bool HasWebsiteLeaderboard => !string.IsNullOrWhiteSpace(Brand.GetLeaderboardUrl());
    public string DonationLinkTitle => Config.Features.DonationRewardsEnabled
        ? $"Get a {Brand.GiftName} {Brand.GiftEmoji} for each tip" : $"Support {Brand.Name} on Ko-fi";
    public string VotingLinkTitle => Config.Features.VotingRewardsEnabled
        ? $"Vote on top.gg and receive 5 {Brand.GiftPluralName} {Brand.GiftEmoji}" : "Vote on top.gg";
    public Color Primary { get; }
    public Color Success { get; }
    public Color Warning { get; }
    public Color Error { get; }
    public Color Information { get; }
    public IEmote Jump { get; }
    public IDictionary<IEmote, PaginatorAction> PaginationEmotes { get; }
    public string TroubleshootingHelp => string.IsNullOrWhiteSpace(Brand.GetTroubleshootingUrl()) ? string.Empty :
        $"If the issue persists, see the [troubleshooting guide]({Brand.GetTroubleshootingUrl()}).";

    public BotPresentation(BotEnvConfig? config = null)
    {
        Config = config ?? new();
        Primary = ParseColor(Config.Theme.Primary, "Primary");
        Success = ParseColor(Config.Theme.Success, "Success");
        Warning = ParseColor(Config.Theme.Warning, "Warning");
        Error = ParseColor(Config.Theme.Error, "Error");
        Information = ParseColor(Config.Theme.Information, "Information");
        if (string.IsNullOrWhiteSpace(Brand.Name) || string.IsNullOrWhiteSpace(Brand.Description) ||
            string.IsNullOrWhiteSpace(Brand.GiftName) || string.IsNullOrWhiteSpace(Brand.GiftPluralName))
            throw new ArgumentException("Branding name, description and gift names must not be empty.");
        foreach (var property in typeof(Branding).GetProperties().Where(p => p.Name.EndsWith("Url")))
        {
            var value = (string)property.GetValue(Brand)!;
            if (!string.IsNullOrWhiteSpace(value) &&
                (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme is not ("https" or "http")))
                throw new ArgumentException($"Branding:{property.Name} must be an absolute HTTP(S) URL or empty.");
        }
        foreach (var name in new[] { Brand.GiftCommandName, Brand.AboutCommandName, Brand.GiftLeaderboardName })
            if (!Regex.IsMatch(name, @"\A[a-z0-9_-]{1,32}\z", RegexOptions.CultureInvariant))
                throw new ArgumentException("Branding command names must contain 1–32 lowercase letters, digits, underscores or hyphens.");
        PaginationEmotes = new Dictionary<IEmote, PaginatorAction>
        {
            [ParseEmote(Config.Pagination.First)] = PaginatorAction.SkipToStart,
            [ParseEmote(Config.Pagination.Previous)] = PaginatorAction.Backward,
            [ParseEmote(Config.Pagination.Next)] = PaginatorAction.Forward,
            [ParseEmote(Config.Pagination.Last)] = PaginatorAction.SkipToEnd
        };
        if (PaginationEmotes.Count != 4)
            throw new ArgumentException("Pagination emoji must be distinct.");
        Jump = ParseEmote(Config.Pagination.Jump);
        if (PaginationEmotes.ContainsKey(Jump))
            throw new ArgumentException("Pagination jump emoji must be distinct.");
    }

    private static Color ParseColor(string value, string role)
    {
        var hex = value.StartsWith('#') ? value[1..] : value;
        if (!Regex.IsMatch(hex, @"\A[0-9a-fA-F]{6}\z", RegexOptions.CultureInvariant))
            throw new ArgumentException($"Theme:{role} must be a six-digit hexadecimal colour.");
        return new Color(uint.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
    }

    private static IEmote ParseEmote(string value)
    {
        if (Emote.TryParse(value, out var emote)) return emote;
        if (Emoji.TryParse(value, out var emoji)) return emoji;
        throw new ArgumentException($"Invalid pagination emoji: {value}");
    }

    public ResponseModel ErrorEmbed(string title, string? description = null) =>
        GenericEmbedService.ErrorEmbed(title, description, this);

    public static string Link(string url, string label) =>
        string.IsNullOrWhiteSpace(url) ? string.Empty : $"[{label}]({url})";

    public static void AddLink(EmbedBuilder embed, string title, string url)
    {
        if (!string.IsNullOrWhiteSpace(url)) embed.AddField(title, url);
    }
}
