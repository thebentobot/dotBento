using Discord;
using dotBento.Bot.Enums;
using dotBento.Bot.Models.Discord;
using dotBento.Domain.Enums;
using dotBento.Infrastructure.Services;
using dotBento.Infrastructure.Utilities;

namespace dotBento.Bot.Commands.SharedCommands;

public sealed class ProfileEditCommand(ProfileService profileService)
{
    public async Task<ResponseModel> SetBackgroundUrlAsync(ulong userId, string url)
    {
        if (!ProfileValidationUtilities.IsValidHttpUrl(url))
        {
            return Error("Invalid URL", "Please provide a valid http/https URL to an image.");
        }

        await profileService.CreateOrUpdateProfileAsync((long)userId, p =>
        {
            p.BackgroundUrl = url;
        });

        return Ok("Background URL updated", $"Background image was set to:{url}");
    }

    public async Task<ResponseModel> SetLastFmBoardAsync(ulong userId, bool enabled)
    {
        await profileService.CreateOrUpdateProfileAsync((long)userId, p =>
        {
            p.LastfmBoard = enabled;
        });

        return Ok("Last.fm board updated", $"Last.fm board is now {(enabled ? "enabled" : "disabled")}.");
    }

    public async Task<ResponseModel> SetXpBoardAsync(ulong userId, bool enabled)
    {
        await profileService.CreateOrUpdateProfileAsync((long)userId, p =>
        {
            p.XpBoard = enabled;
        });

        return Ok("XP board updated", $"XP board is now {(enabled ? "enabled" : "disabled")}.");
    }

    public async Task<ResponseModel> SetBackgroundColourAsync(ulong userId, string hex, int? opacity)
    {
        var normalised = ProfileValidationUtilities.NormalizeHex(hex);
        if (normalised == null)
        {
            return Error("Invalid colour", "Please provide a valid hex colour like #1F2937 or 1F2937.");
        }

        if (opacity is < 0 or > 100)
        {
            return Error("Invalid opacity", "Opacity must be between 0 and 100.");
        }

        await profileService.CreateOrUpdateProfileAsync((long)userId, p =>
        {
            p.BackgroundColour = normalised;
            if (opacity.HasValue) p.BackgroundColourOpacity = opacity.Value;
        });

        return Ok("Background colour updated",
            $"Colour set to `{normalised}`{(opacity.HasValue ? $", opacity `{opacity.Value}%`" : string.Empty)}.");
    }

    public async Task<ResponseModel> SetDescriptionAsync(ulong userId, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Error("Invalid description", "Description cannot be empty.");
        }
        if (text.Length > 500)
        {
            return Error("Too long", "Description must be 500 characters or fewer.");
        }

        await profileService.CreateOrUpdateProfileAsync((long)userId, p =>
        {
            p.Description = text;
        });

        return Ok("Description updated", "Your profile description was updated to the following:\n\n" + text);
    }

    public async Task<ResponseModel> SetTimezoneAsync(ulong userId, string id)
    {
        if (!ProfileValidationUtilities.TryValidateTimezone(id))
        {
            return Error("Invalid timezone", "Please provide a valid timezone ID (example: Europe/Oslo).");
        }

        await profileService.CreateOrUpdateProfileAsync((long)userId, p =>
        {
            p.Timezone = id;
        });

        return Ok("Timezone updated", $"Your timezone was set to: `{id}`");
    }

    public async Task<ResponseModel> SetBirthdayAsync(ulong userId, string date)
    {
        if (!ProfileValidationUtilities.TryParseBirthday(date, out var stored))
        {
            return Error("Invalid date", "Please provide your birthday as month-day, like 07-21 or 7/21.");
        }

        await profileService.CreateOrUpdateProfileAsync((long)userId, p =>
        {
            p.Birthday = stored;
        });

        return Ok("Birthday updated", $"Your birthday was set to: {DateTime.Parse(stored):MMM d}");
    }

    public async Task<ResponseModel> ResetBackgroundAsync(ulong userId)
    {
        await profileService.CreateOrUpdateProfileAsync((long)userId, p =>
        {
            p.BackgroundUrl = null;
            p.BackgroundColour = null;
            p.BackgroundColourOpacity = null;
        });
        return Ok("Background reset", "Your background image and colour settings have been reset to default.");
    }

    public async Task<ResponseModel> ResetDescriptionAsync(ulong userId)
    {
        await profileService.CreateOrUpdateProfileAsync((long)userId, p => { p.Description = null; });
        return Ok("Description reset", "Your profile description has been cleared.");
    }

    public async Task<ResponseModel> ResetTimezoneAsync(ulong userId)
    {
        await profileService.CreateOrUpdateProfileAsync((long)userId, p => { p.Timezone = null; });
        return Ok("Timezone reset", "Your timezone has been cleared.");
    }

    public async Task<ResponseModel> ResetBirthdayAsync(ulong userId)
    {
        await profileService.CreateOrUpdateProfileAsync((long)userId, p => { p.Birthday = null; });
        return Ok("Birthday reset", "Your birthday has been cleared.");
    }

    private static ResponseModel Ok(string title, string description)
    {
        var response = new ResponseModel { ResponseType = ResponseType.Embed };
        response.Embed
            .WithColor(Color.Green)
            .WithTitle(title)
            .WithDescription(description);
        response.CommandResponse = CommandResponse.Ok;
        return response;
    }

    private static ResponseModel Error(string title, string description)
    {
        var response = new ResponseModel { ResponseType = ResponseType.Embed };
        response.Embed
            .WithColor(Color.Red)
            .WithTitle(title)
            .WithDescription(description);
        response.CommandResponse = CommandResponse.Error;
        return response;
    }
}