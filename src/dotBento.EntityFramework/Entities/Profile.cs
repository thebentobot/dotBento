using System;
using System.Collections.Generic;

namespace dotBento.EntityFramework.Entities;

public partial class Profile
{
    public long UserId { get; set; }

    public bool? LastfmBoard { get; set; }

    public bool? XpBoard { get; set; }

    public string? BackgroundUrl { get; set; }

    public int? BackgroundColourOpacity { get; set; }

    public string? BackgroundColour { get; set; }

    public int? DescriptionColourOpacity { get; set; }

    public string? DescriptionColour { get; set; }

    public int? OverlayOpacity { get; set; }

    public string? OverlayColour { get; set; }

    public string? UsernameColour { get; set; }

    public string? DiscriminatorColour { get; set; }

    public string? SidebarItemServerColour { get; set; }

    public string? SidebarItemGlobalColour { get; set; }

    public string? SidebarItemBentoColour { get; set; }

    public string? SidebarItemTimezoneColour { get; set; }

    public string? SidebarValueServerColour { get; set; }

    public string? SidebarValueGlobalColour { get; set; }

    public string? SidebarValueBentoColour { get; set; }

    public int? SidebarOpacity { get; set; }

    public string? SidebarColour { get; set; }

    public int? SidebarBlur { get; set; }

    public int? FmDivBgopacity { get; set; }

    public string? FmDivBgcolour { get; set; }

    public int? FmSongTextOpacity { get; set; }

    public string? FmSongTextColour { get; set; }

    public int? FmArtistTextOpacity { get; set; }

    public string? FmArtistTextColour { get; set; }

    public int? XpDivBgopacity { get; set; }

    public string? XpDivBgcolour { get; set; }

    public int? XpTextOpacity { get; set; }

    public string? XpTextColour { get; set; }

    public int? XpText2Opacity { get; set; }

    public string? XpText2Colour { get; set; }

    public int? XpDoneServerColour1Opacity { get; set; }

    public string? XpDoneServerColour1 { get; set; }

    public int? XpDoneServerColour2Opacity { get; set; }

    public string? XpDoneServerColour2 { get; set; }

    public int? XpDoneServerColour3Opacity { get; set; }

    public string? XpDoneServerColour3 { get; set; }

    public int? XpDoneGlobalColour1Opacity { get; set; }

    public string? XpDoneGlobalColour1 { get; set; }

    public int? XpDoneGlobalColour2Opacity { get; set; }

    public string? XpDoneGlobalColour2 { get; set; }

    public int? XpDoneGlobalColour3Opacity { get; set; }

    public string? XpDoneGlobalColour3 { get; set; }

    public string? Description { get; set; }

    public string? Timezone { get; set; }

    public string? Birthday { get; set; }

    public int? XpBarOpacity { get; set; }

    public string? XpBarColour { get; set; }

    public int? XpBar2Opacity { get; set; }

    public string? XpBar2Colour { get; set; }

    public virtual User User { get; set; } = null!;
}
