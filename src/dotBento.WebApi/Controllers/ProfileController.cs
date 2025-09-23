using dotBento.EntityFramework.Context;
using dotBento.EntityFramework.Entities;
using dotBento.Infrastructure.Utilities;
using dotBento.WebApi.Dtos;
using dotBento.WebApi.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dotBento.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class ProfileController(BotDbContext dbContext, IMemoryCache cache) : ControllerBase
{
    [HttpGet("{userId:long}")]
    public async Task<ActionResult<ProfileDto>> GetProfile(long userId)
    {
        var bentoUser = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);

        if (bentoUser == null)
        {
            return NotFound();
        }

        var profile = await dbContext.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null)
        {
            return NotFound();
        }

        return Ok(profile.ToProfileDto());
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

        var profile = await dbContext.Profiles.FirstOrDefaultAsync(p => p.UserId == request.UserId);
        var isNew = false;
        if (profile == null)
        {
            profile = new Profile { UserId = request.UserId };
            isNew = true;
        }

        if (request.LastfmBoard.HasValue) profile.LastfmBoard = request.LastfmBoard.Value;
        if (request.XpBoard.HasValue) profile.XpBoard = request.XpBoard.Value;

        var applyError = TryApplyProfileChanges(profile, request);
        if (applyError != null) return applyError;

        if (isNew)
            await dbContext.Profiles.AddAsync(profile);
        else
            dbContext.Profiles.Update(profile);

        await dbContext.SaveChangesAsync();

        // Invalidate cache so bot side reads fresh data
        cache.Remove($"profile:{request.UserId}");

        return Ok(profile.ToProfileDto());
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