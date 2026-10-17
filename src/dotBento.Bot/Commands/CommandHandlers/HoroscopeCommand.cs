using Discord;
using Discord.WebSocket;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Resources;
using dotBento.Infrastructure.Models.Horoscope;
using dotBento.Infrastructure.Services;
using dotBento.Infrastructure.Services.Api;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Commands.CommandHandlers;

public sealed class HoroscopeCommand(
    HoroscopeService horoscopeService,
    BentoMediaServerService mediaServerService,
    IOptions<BotEnvConfig> config)
{
    private static readonly Color HoroscopePurple = new(0x9266CC);

    private static readonly IReadOnlyDictionary<string, ZodiacInfo> ZodiacSigns =
        new Dictionary<string, ZodiacInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["aries"] = new("Aries", "♈", "Mar 21 – Apr 19"),
            ["taurus"] = new("Taurus", "♉", "Apr 20 – May 20"),
            ["gemini"] = new("Gemini", "♊", "May 21 – Jun 20"),
            ["cancer"] = new("Cancer", "♋", "Jun 21 – Jul 22"),
            ["leo"] = new("Leo", "♌", "Jul 23 – Aug 22"),
            ["virgo"] = new("Virgo", "♍", "Aug 23 – Sep 22"),
            ["libra"] = new("Libra", "♎", "Sep 23 – Oct 22"),
            ["scorpio"] = new("Scorpio", "♏", "Oct 23 – Nov 21"),
            ["sagittarius"] = new("Sagittarius", "♐", "Nov 22 – Dec 21"),
            ["capricorn"] = new("Capricorn", "♑", "Dec 22 – Jan 19"),
            ["aquarius"] = new("Aquarius", "♒", "Jan 20 – Feb 18"),
            ["pisces"] = new("Pisces", "♓", "Feb 19 – Mar 20")
        };

    public async Task<ResponseModel> GetHoroscopeAsync(
        long userId, string window, string? sign = null, IChannel? channel = null)
    {
        var normalizedWindow = NormalizeWindow(window);
        if (normalizedWindow is null)
            return Error("Invalid horoscope period", "Use yesterday, today, tomorrow, weekly, or monthly.");

        var normalizedSign = NormalizeSign(sign);
        if (sign is not null && normalizedSign is null)
            return InvalidSign(sign);

        if (normalizedSign is null)
        {
            var saved = await horoscopeService.GetHoroscopeAsync(userId);
            if (saved.HasNoValue)
            {
                return Error(
                    "No zodiac sign saved",
                    "Choose your sign in `/horoscope settings` so you won't need to select it every time, or choose a sign for this reading.");
            }
            normalizedSign = NormalizeSign(saved.Value.Sign);
            if (normalizedSign is null)
                return InvalidSign(saved.Value.Sign);
        }

        var mediaConfig = config.Value.MediaServer;
        if (string.IsNullOrWhiteSpace(mediaConfig.Url))
        {
            return Error(
                "Horoscope commands are not enabled",
                "This bot instance has not been configured with bento-media-server. Contact the bot operator.",
                Color.Orange);
        }

        var apiKey = string.IsNullOrWhiteSpace(mediaConfig.ApiKey) ? null : mediaConfig.ApiKey;
        var result = await mediaServerService.GetHoroscopeAsync(
            mediaConfig.Url,
            normalizedSign,
            normalizedWindow,
            apiKey);
        return result.IsFailure
            ? Error("Failed to fetch horoscope", result.Error)
            : BuildReading(result.Value, IsAgeRestrictedChannel(channel));
    }

    public async Task<ResponseModel> SaveHoroscopeAsync(long userId, string? sign)
    {
        var normalizedSign = NormalizeSign(sign);
        if (normalizedSign is null)
            return InvalidSign(sign ?? string.Empty);

        await horoscopeService.SaveHoroscopeAsync(userId, normalizedSign);
        var info = ZodiacSigns[normalizedSign];
        return await GetSettingsAsync(userId, $"Your zodiac sign is now **{info.DisplayName}**.");
    }

    public async Task<ResponseModel> RemoveHoroscopeAsync(long userId)
    {
        await horoscopeService.DeleteHoroscopeAsync(userId);
        return await GetSettingsAsync(userId, "Your saved zodiac sign was removed.");
    }

    public async Task<ResponseModel> GetSettingsAsync(long userId, string? notice = null)
    {
        var saved = await horoscopeService.GetHoroscopeAsync(userId);
        var sign = saved.HasValue ? NormalizeSign(saved.Value.Sign) : null;
        var current = sign is null ? "No zodiac sign saved." :
            $"Saved sign: {ZodiacSigns[sign].Emoji} **{ZodiacSigns[sign].DisplayName}**";
        var description = $"{current}\n\nChoose your zodiac sign below to save it. " +
                          "You won't need to select it every time you use `/horoscope today` or another reading command. " +
                          "You can still choose a different sign for a one-off reading without changing your saved sign.";
        if (notice is not null)
            description = $"{notice}\n\n{description}";

        var select = new SelectMenuBuilder()
            .WithCustomId($"horoscope-settings:sign:{userId}")
            .WithPlaceholder("Choose your zodiac sign")
            .WithMinValues(1)
            .WithMaxValues(1);
        foreach (var (value, info) in ZodiacSigns)
        {
            select.AddOption(info.DisplayName, value, info.DateRange,
                new Emoji(info.Emoji), isDefault: value == sign);
        }

        return new ResponseModel
        {
            ResponseType = ResponseType.Embed,
            Embed = new EmbedBuilder()
                .WithColor(HoroscopePurple)
                .WithTitle("🔮 Horoscope settings")
                .WithDescription(description),
            Components = new ComponentBuilder()
                .WithSelectMenu(select)
                .WithButton("Remove saved sign", $"horoscope-settings:remove:{userId}",
                    ButtonStyle.Secondary, disabled: sign is null, row: 1)
        };
    }

    public static string? NormalizeWindow(string window) => window.Trim().ToLowerInvariant() switch
    {
        "yesterday" => "yesterday",
        "today" => "today",
        "tomorrow" => "tomorrow",
        "week" or "weekly" => "weekly",
        "month" or "monthly" => "monthly",
        _ => null
    };

    private static string? NormalizeSign(string? sign)
    {
        if (string.IsNullOrWhiteSpace(sign))
            return null;
        var normalized = sign.Trim().ToLowerInvariant();
        return ZodiacSigns.ContainsKey(normalized) ? normalized : null;
    }

    public static bool IsAgeRestrictedChannel(IChannel? channel) => channel switch
    {
        SocketThreadChannel thread => IsAgeRestrictedChannel(thread.ParentChannel),
        ITextChannel text => text.IsNsfw,
        IForumChannel forum => forum.IsNsfw,
        _ => false
    };

    private static ResponseModel BuildReading(HoroscopeResponse horoscope, bool includeRomance)
    {
        var info = ZodiacSigns[horoscope.Zodiac];
        var temporalLabel = horoscope.Window switch
        {
            "yesterday" => "Yesterday",
            "today" => "Today",
            "tomorrow" => "Tomorrow",
            "weekly" => "This week",
            "monthly" => "This month",
            _ => horoscope.Window
        };
        var title = Truncate($"{info.Emoji} {info.DisplayName} | {temporalLabel} · {horoscope.Label}", 256);
        var fields = horoscope.Aspects
            .Where(aspect => includeRomance || !string.Equals(aspect.Name, "Romance", StringComparison.OrdinalIgnoreCase))
            .Select(aspect =>
            {
                var score = Math.Clamp(aspect.Score, 0, 5);
                var stars = new string('★', score) + new string('☆', 5 - score);
                return new EmbedFieldBuilder()
                    .WithName(Truncate($"{aspect.Name} {stars}", 256))
                    .WithValue(Truncate(aspect.Detail, 1024))
                    .WithIsInline(false);
            }).ToList();

        var embeds = SplitReading(horoscope.Reading, 4096)
            .Select(reading => BuildEmbed(title, reading)).ToList();
        // Leave room for the page footer, including on pages containing only aspect fields.
        const int contentBudget = 6000 - 64;
        foreach (var field in fields)
        {
            var page = embeds[^1];
            var pageLength = title.Length + (page.Description?.Length ?? 0) +
                             page.Fields.Sum(existing => existing.Name.Length + existing.Value.ToString()!.Length);
            var fieldLength = field.Name.Length + field.Value.ToString()!.Length;
            if (page.Fields.Count == 25 || pageLength + fieldLength > contentBudget)
            {
                page = BuildEmbed(title, string.Empty);
                embeds.Add(page);
            }
            page.AddField(field);
        }

        if (embeds.Count == 1)
            return new ResponseModel { ResponseType = ResponseType.Embed, Embed = embeds[0] };

        var pages = embeds.Select((embed, index) =>
        {
            embed.WithFooter($"{index + 1} / {embeds.Count}");
            return PageBuilder.FromEmbedBuilder(embed);
        }).ToList();

        return new ResponseModel
        {
            ResponseType = ResponseType.Paginator,
            StaticPaginator = pages.BuildSimpleStaticPaginator(),
            PaginatorTimeout = TimeSpan.FromMinutes(5)
        };
    }

    private static EmbedBuilder BuildEmbed(string title, string reading) => new EmbedBuilder()
        .WithColor(HoroscopePurple)
        .WithTitle(title)
        .WithDescription(reading);

    private static List<string> SplitReading(string reading, int limit)
    {
        var pages = new List<string>();
        var remaining = reading.Trim();
        while (remaining.Length > limit)
        {
            var split = remaining.LastIndexOfAny(['\n', ' '], limit, limit);
            if (split <= 0)
                split = limit;
            pages.Add(remaining[..split].Trim());
            remaining = remaining[split..].Trim();
        }
        pages.Add(remaining);
        return pages;
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..(maxLength - 1)] + "…";

    private static ResponseModel InvalidSign(string sign) => Error(
        "Invalid zodiac sign",
        $"`{sign}` is not a valid zodiac sign. Choose one in `/horoscope settings` or the reading command's sign dropdown.");

    private static ResponseModel Error(string title, string description, Color? color = null) => new()
    {
        ResponseType = ResponseType.Embed,
        Embed = new EmbedBuilder()
            .WithColor(color ?? DiscordConstants.ErrorRed)
            .WithTitle(title)
            .WithDescription(description)
    };

    private sealed record ZodiacInfo(string DisplayName, string Emoji, string DateRange);
}
