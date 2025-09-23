using dotBento.EntityFramework.Context;
using dotBento.EntityFramework.Entities;
using dotBento.Infrastructure.Utilities;
using dotBento.WebApi.Dtos;
using dotBento.WebApi.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using dotBento.Infrastructure.Services;

namespace dotBento.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class ProfileController(BotDbContext dbContext, ProfileService profileService) : ControllerBase
{
    [HttpGet("{userId:long}")]
    public async Task<ActionResult<ProfileDto>> GetProfile(long userId)
    {
        var bentoUser = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
        if (bentoUser == null)
            return NotFound();

        var maybe = await profileService.GetProfileAsync(userId);
        if (maybe.HasNoValue)
            return NotFound();
        return Ok(maybe.Value.ToProfileDto());
    }

    [HttpPost]
    public async Task<ActionResult<ProfileDto>> UpsertProfile([FromBody] ProfileUpdateRequest? request)
    {
        if (request == null || request.UserId == 0)
        {
            return BadRequest("UserId must be provided");
        }

        var bentoUser = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == request.UserId);
        if (bentoUser == null)
        {
            return NotFound("User does not exist in the Bento database.");
        }

        var maybeExisting = await profileService.GetProfileAsync(request.UserId);
        var working = maybeExisting.HasValue ? maybeExisting.Value : new Profile { UserId = request.UserId };

        if (request.LastfmBoard.HasValue) working.LastfmBoard = request.LastfmBoard.Value;
        if (request.XpBoard.HasValue) working.XpBoard = request.XpBoard.Value;

        var applyErr = TryApplyProfileChanges(working, request);
        if (applyErr != null) return applyErr;

        var saved = await profileService.CreateOrUpdateProfileAsync(request.UserId, p =>
        {
            p.LastfmBoard = working.LastfmBoard;
            p.XpBoard = working.XpBoard;
            p.BackgroundUrl = working.BackgroundUrl;
            p.BackgroundColourOpacity = working.BackgroundColourOpacity;
            p.BackgroundColour = working.BackgroundColour;
            p.DescriptionColourOpacity = working.DescriptionColourOpacity;
            p.DescriptionColour = working.DescriptionColour;
            p.OverlayOpacity = working.OverlayOpacity;
            p.OverlayColour = working.OverlayColour;
            p.UsernameColour = working.UsernameColour;
            p.DiscriminatorColour = working.DiscriminatorColour;
            p.SidebarItemServerColour = working.SidebarItemServerColour;
            p.SidebarItemGlobalColour = working.SidebarItemGlobalColour;
            p.SidebarItemBentoColour = working.SidebarItemBentoColour;
            p.SidebarItemTimezoneColour = working.SidebarItemTimezoneColour;
            p.SidebarValueServerColour = working.SidebarValueServerColour;
            p.SidebarValueGlobalColour = working.SidebarValueGlobalColour;
            p.SidebarValueBentoColour = working.SidebarValueBentoColour;
            p.SidebarOpacity = working.SidebarOpacity;
            p.SidebarColour = working.SidebarColour;
            p.SidebarBlur = working.SidebarBlur;
            p.FmDivBgopacity = working.FmDivBgopacity;
            p.FmDivBgcolour = working.FmDivBgcolour;
            p.FmSongTextOpacity = working.FmSongTextOpacity;
            p.FmSongTextColour = working.FmSongTextColour;
            p.FmArtistTextOpacity = working.FmArtistTextOpacity;
            p.FmArtistTextColour = working.FmArtistTextColour;
            p.XpDivBgopacity = working.XpDivBgopacity;
            p.XpDivBgcolour = working.XpDivBgcolour;
            p.XpTextOpacity = working.XpTextOpacity;
            p.XpTextColour = working.XpTextColour;
            p.XpText2Opacity = working.XpText2Opacity;
            p.XpText2Colour = working.XpText2Colour;
            p.XpDoneServerColour1Opacity = working.XpDoneServerColour1Opacity;
            p.XpDoneServerColour1 = working.XpDoneServerColour1;
            p.XpDoneServerColour2Opacity = working.XpDoneServerColour2Opacity;
            p.XpDoneServerColour2 = working.XpDoneServerColour2;
            p.XpDoneServerColour3Opacity = working.XpDoneServerColour3Opacity;
            p.XpDoneServerColour3 = working.XpDoneServerColour3;
            p.XpDoneGlobalColour1Opacity = working.XpDoneGlobalColour1Opacity;
            p.XpDoneGlobalColour1 = working.XpDoneGlobalColour1;
            p.XpDoneGlobalColour2Opacity = working.XpDoneGlobalColour2Opacity;
            p.XpDoneGlobalColour2 = working.XpDoneGlobalColour2;
            p.XpDoneGlobalColour3Opacity = working.XpDoneGlobalColour3Opacity;
            p.XpDoneGlobalColour3 = working.XpDoneGlobalColour3;
            p.Description = working.Description;
            p.Timezone = working.Timezone;
            p.Birthday = working.Birthday;
            p.XpBarOpacity = working.XpBarOpacity;
            p.XpBarColour = working.XpBarColour;
            p.XpBar2Opacity = working.XpBar2Opacity;
            p.XpBar2Colour = working.XpBar2Colour;
        });

        return Ok(saved.ToProfileDto());
    }

    private ActionResult? TryApplyProfileChanges(Profile profile, ProfileUpdateRequest request)
    {
        // Background URL
        if (!TryApplyUrl(request.BackgroundUrl, "Invalid BackgroundUrl. Must be an http/https URL.",
                v => profile.BackgroundUrl = v, out var err))
            return BadRequest(err);

        // Background colour + opacity
        if (!TryApplyOpacity(request.BackgroundColourOpacity, "BackgroundColourOpacity must be between 0 and 100.",
                v => profile.BackgroundColourOpacity = v, out err))
            return BadRequest(err);
        if (!TryApplyHex(request.BackgroundColour, "Invalid BackgroundColour. Must be a hex colour like #1F2937.",
                v => profile.BackgroundColour = v, out err))
            return BadRequest(err);

        // Description
        if (request.Description != null)
        {
            if (request.Description.Length > 500)
                return BadRequest("Description must be 500 characters or fewer.");
            profile.Description = request.Description;
        }

        // Timezone
        if (request.Timezone != null)
        {
            if (!ProfileValidationUtilities.TryValidateTimezone(request.Timezone))
                return BadRequest("Invalid Timezone. Provide a valid timezone ID (e.g., Europe/Oslo).");
            profile.Timezone = request.Timezone;
        }

        // Birthday
        if (request.Birthday != null)
        {
            if (!ProfileValidationUtilities.TryParseBirthday(request.Birthday, out var normalized))
                return BadRequest("Invalid Birthday. Provide month-day like 07-21 or 7/21.");
            profile.Birthday = normalized;
        }

        // Description colour + opacity
        if (!TryApplyOpacity(request.DescriptionColourOpacity, "DescriptionColourOpacity must be between 0 and 100.",
                v => profile.DescriptionColourOpacity = v, out err) || !TryApplyHex(request.DescriptionColour,
                "Invalid DescriptionColour. Must be hex #RRGGBB.",
                v => profile.DescriptionColour = v, out err))
            return BadRequest(err);

        // Overlay
        if (!TryApplyOpacity(request.OverlayOpacity, "OverlayOpacity must be between 0 and 100.",
                v => profile.OverlayOpacity = v, out err))
            return BadRequest(err);
        if (!TryApplyHex(request.OverlayColour, "Invalid OverlayColour. Must be hex #RRGGBB.",
                v => profile.OverlayColour = v, out err))
            return BadRequest(err);

        // Username/Discriminator colours
        if (!TryApplyHex(request.UsernameColour, "Invalid UsernameColour. Must be hex #RRGGBB.",
                v => profile.UsernameColour = v, out err))
            return BadRequest(err);
        if (!TryApplyHex(request.DiscriminatorColour, "Invalid DiscriminatorColour. Must be hex #RRGGBB.",
                v => profile.DiscriminatorColour = v, out err))
            return BadRequest(err);

        // Sidebar item colours
        if (!TryApplyHex(request.SidebarItemServerColour, "Invalid SidebarItemServerColour.",
                v => profile.SidebarItemServerColour = v, out err))
            return BadRequest(err);
        if (!TryApplyHex(request.SidebarItemGlobalColour, "Invalid SidebarItemGlobalColour.",
                v => profile.SidebarItemGlobalColour = v, out err) ||
            !TryApplyHex(request.SidebarItemBentoColour, "Invalid SidebarItemBentoColour.",
                v => profile.SidebarItemBentoColour = v, out err) || !TryApplyHex(request.SidebarItemTimezoneColour,
                "Invalid SidebarItemTimezoneColour.", v => profile.SidebarItemTimezoneColour = v, out err))
            return BadRequest(err);

        // Sidebar value colours
        if (!TryApplyHex(request.SidebarValueServerColour, "Invalid SidebarValueServerColour.",
                v => profile.SidebarValueServerColour = v, out err))
            return BadRequest(err);
        if (!TryApplyHex(request.SidebarValueGlobalColour, "Invalid SidebarValueGlobalColour.",
                v => profile.SidebarValueGlobalColour = v, out err) || !TryApplyHex(request.SidebarValueBentoColour,
                "Invalid SidebarValueBentoColour.", v => profile.SidebarValueBentoColour = v, out err))
            return BadRequest(err);

        // Sidebar opacity/colour/blur
        if (!TryApplyOpacity(request.SidebarOpacity, "SidebarOpacity must be between 0 and 100.",
                v => profile.SidebarOpacity = v, out err))
            return BadRequest(err);
        if (!TryApplyHex(request.SidebarColour, "Invalid SidebarColour.", v => profile.SidebarColour = v, out err) ||
            !TryApplyNonNegativeInt(request.SidebarBlur, "SidebarBlur cannot be negative.",
                v => profile.SidebarBlur = v, out err))
            return BadRequest(err);

        // FM section
        if (!TryApplyOpacity(request.FmDivBgopacity, "FmDivBgopacity must be between 0 and 100.",
                v => profile.FmDivBgopacity = v, out err))
            return BadRequest(err);
        if (!TryApplyHex(request.FmDivBgcolour, "Invalid FmDivBgcolour.", v => profile.FmDivBgcolour = v, out err) ||
            !TryApplyOpacity(request.FmSongTextOpacity, "FmSongTextOpacity must be between 0 and 100.",
                v => profile.FmSongTextOpacity = v, out err) ||
            !TryApplyHex(request.FmSongTextColour, "Invalid FmSongTextColour.", v => profile.FmSongTextColour = v,
                out err) ||
            !TryApplyOpacity(request.FmArtistTextOpacity, "FmArtistTextOpacity must be between 0 and 100.",
                v => profile.FmArtistTextOpacity = v, out err) || !TryApplyHex(request.FmArtistTextColour,
                "Invalid FmArtistTextColour.", v => profile.FmArtistTextColour = v, out err))
            return BadRequest(err);

        // XP section (div bg)
        if (!TryApplyOpacity(request.XpDivBgopacity, "XpDivBgopacity must be between 0 and 100.",
                v => profile.XpDivBgopacity = v, out err))
            return BadRequest(err);
        if (!TryApplyHex(request.XpDivBgcolour, "Invalid XpDivBgcolour.", v => profile.XpDivBgcolour = v, out err))
            return BadRequest(err);

        // XP text
        if (!TryApplyOpacity(request.XpTextOpacity, "XpTextOpacity must be between 0 and 100.",
                v => profile.XpTextOpacity = v, out err))
            return BadRequest(err);
        if (!TryApplyHex(request.XpTextColour, "Invalid XpTextColour.", v => profile.XpTextColour = v, out err) ||
            !TryApplyOpacity(request.XpText2Opacity, "XpText2Opacity must be between 0 and 100.",
                v => profile.XpText2Opacity = v, out err) || !TryApplyHex(request.XpText2Colour,
                "Invalid XpText2Colour.", v => profile.XpText2Colour = v, out err))
            return BadRequest(err);

        // XP done server colours
        if (!TryApplyOpacity(request.XpDoneServerColour1Opacity,
                "XpDoneServerColour1Opacity must be between 0 and 100.", v => profile.XpDoneServerColour1Opacity = v,
                out err))
            return BadRequest(err);
        if (!TryApplyHex(request.XpDoneServerColour1, "Invalid XpDoneServerColour1.",
                v => profile.XpDoneServerColour1 = v, out err) ||
            !TryApplyOpacity(request.XpDoneServerColour2Opacity,
                "XpDoneServerColour2Opacity must be between 0 and 100.", v => profile.XpDoneServerColour2Opacity = v,
                out err) ||
            !TryApplyHex(request.XpDoneServerColour2, "Invalid XpDoneServerColour2.",
                v => profile.XpDoneServerColour2 = v, out err) ||
            !TryApplyOpacity(request.XpDoneServerColour3Opacity,
                "XpDoneServerColour3Opacity must be between 0 and 100.", v => profile.XpDoneServerColour3Opacity = v,
                out err) || !TryApplyHex(request.XpDoneServerColour3, "Invalid XpDoneServerColour3.",
                v => profile.XpDoneServerColour3 = v, out err))
            return BadRequest(err);

        // XP done global colours
        if (!TryApplyOpacity(request.XpDoneGlobalColour1Opacity,
                "XpDoneGlobalColour1Opacity must be between 0 and 100.", v => profile.XpDoneGlobalColour1Opacity = v,
                out err))
            return BadRequest(err);
        if (!TryApplyHex(request.XpDoneGlobalColour1, "Invalid XpDoneGlobalColour1.",
                v => profile.XpDoneGlobalColour1 = v, out err) ||
            !TryApplyOpacity(request.XpDoneGlobalColour2Opacity,
                "XpDoneGlobalColour2Opacity must be between 0 and 100.", v => profile.XpDoneGlobalColour2Opacity = v,
                out err) ||
            !TryApplyHex(request.XpDoneGlobalColour2, "Invalid XpDoneGlobalColour2.",
                v => profile.XpDoneGlobalColour2 = v, out err) ||
            !TryApplyOpacity(request.XpDoneGlobalColour3Opacity,
                "XpDoneGlobalColour3Opacity must be between 0 and 100.", v => profile.XpDoneGlobalColour3Opacity = v,
                out err) || !TryApplyHex(request.XpDoneGlobalColour3, "Invalid XpDoneGlobalColour3.",
                v => profile.XpDoneGlobalColour3 = v, out err))
            return BadRequest(err);

        // XP bar colours
        if (!TryApplyOpacity(request.XpBarOpacity, "XpBarOpacity must be between 0 and 100.",
                v => profile.XpBarOpacity = v, out err))
            return BadRequest(err);
        if (!TryApplyHex(request.XpBarColour, "Invalid XpBarColour.", v => profile.XpBarColour = v, out err) ||
            !TryApplyOpacity(request.XpBar2Opacity, "XpBar2Opacity must be between 0 and 100.",
                v => profile.XpBar2Opacity = v, out err) || !TryApplyHex(request.XpBar2Colour, "Invalid XpBar2Colour.",
                v => profile.XpBar2Colour = v, out err))
            return BadRequest(err);

        return null;
    }

    private static bool TryApplyOpacity(int? value, string errorMessage, Action<int> assign, out string? error)
    {
        error = null;
        switch (value)
        {
            case null:
                return true;
            case < 0 or > 100:
                error = errorMessage;
                return false;
            default:
                assign(value.Value);
                return true;
        }
    }

    private static bool TryApplyNonNegativeInt(int? value, string errorMessage, Action<int> assign, out string? error)
    {
        error = null;
        switch (value)
        {
            case null:
                return true;
            case < 0:
                error = errorMessage;
                return false;
            default:
                assign(value.Value);
                return true;
        }
    }

    private static bool TryApplyHex(string? value, string invalidMessage, Action<string> assign, out string? error)
    {
        error = null;
        if (value == null) return true;
        var norm = ProfileValidationUtilities.NormalizeHex(value);
        if (norm == null)
        {
            error = invalidMessage;
            return false;
        }

        assign(norm);
        return true;
    }

    private static bool TryApplyUrl(string? value, string invalidMessage, Action<string> assign, out string? error)
    {
        error = null;
        if (value == null) return true;
        if (!ProfileValidationUtilities.IsValidHttpUrl(value))
        {
            error = invalidMessage;
            return false;
        }

        assign(value);
        return true;
    }
}