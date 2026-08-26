using Discord;
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

namespace dotBento.Bot.Commands.SharedCommands;

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

    public async Task<ResponseModel> GetHoroscopeAsync(long userId, string window, string? sign = null)
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
                    "Save one with `/horoscope save` or use `horoscope save <sign>`. Use `horoscope list` to see valid signs.");
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
            : BuildReading(result.Value);
    }

    public async Task<ResponseModel> SaveHoroscopeAsync(long userId, string? sign)
    {
        var normalizedSign = NormalizeSign(sign);
        if (normalizedSign is null)
            return InvalidSign(sign ?? string.Empty);

        await horoscopeService.SaveHoroscopeAsync(userId, normalizedSign);
        var info = ZodiacSigns[normalizedSign];
        return new ResponseModel
        {
            ResponseType = ResponseType.Embed,
            Embed = new EmbedBuilder()
                .WithColor(HoroscopePurple)
                .WithTitle($"{info.Emoji} Zodiac saved")
                .WithDescription($"Your zodiac sign is now **{info.DisplayName}**.")
        };
    }

    public async Task<ResponseModel> RemoveHoroscopeAsync(long userId)
    {
        await horoscopeService.DeleteHoroscopeAsync(userId);
        return new ResponseModel
        {
            ResponseType = ResponseType.Embed,
            Embed = new EmbedBuilder()
                .WithColor(HoroscopePurple)
                .WithTitle("Zodiac sign removed")
                .WithDescription("Your saved zodiac sign was removed.")
        };
    }

    public ResponseModel ListHoroscopes()
    {
        var description = string.Join(
            "\n",
            ZodiacSigns.Values.Select(info => $"{info.Emoji} **{info.DisplayName}**: {info.DateRange}"));
        return new ResponseModel
        {
            ResponseType = ResponseType.Embed,
            Embed = new EmbedBuilder()
                .WithColor(HoroscopePurple)
                .WithTitle("🔮 Zodiac signs")
                .WithDescription(description)
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

    private static ResponseModel BuildReading(HoroscopeResponse horoscope)
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
        var title = $"{info.Emoji} {info.DisplayName} | {temporalLabel} · {horoscope.Label}";
        var fields = horoscope.Aspects.Select(aspect =>
        {
            var score = Math.Clamp(aspect.Score, 0, 5);
            var stars = new string('★', score) + new string('☆', 5 - score);
            return new EmbedFieldBuilder()
                .WithName($"{aspect.Name} {stars}")
                .WithValue(Truncate(aspect.Detail, 1024))
                .WithIsInline(false);
        }).ToList();

        var fieldCharacters = fields.Sum(field => field.Name.Length + field.Value.ToString()!.Length);
        var descriptionLimit = Math.Min(4096, Math.Max(500, 6000 - title.Length - fieldCharacters - 64));
        var readingPages = SplitReading(horoscope.Reading, descriptionLimit);

        if (readingPages.Count == 1)
        {
            var embed = BuildEmbed(title, readingPages[0]);
            embed.WithFields(fields);
            return new ResponseModel { ResponseType = ResponseType.Embed, Embed = embed };
        }

        var pages = readingPages.Select((reading, index) =>
        {
            var embed = BuildEmbed(title, reading)
                .WithFooter($"{index + 1} / {readingPages.Count}");
            if (index == readingPages.Count - 1)
                embed.WithFields(fields);
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
        $"`{sign}` is not a valid zodiac sign. Use `horoscope list` to see the twelve valid signs.");

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
