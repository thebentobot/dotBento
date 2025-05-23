using dotBento.Domain.Entities;

namespace dotBento.Infrastructure.Extensions;

public static class ProfileExtensions
{
    public static Profile Map(this EntityFramework.Entities.Profile profile) =>
        new(
            profile.UserId,
            profile.LastfmBoard,
            profile.XpBoard,
            profile.BackgroundUrl,
            profile.BackgroundColourOpacity,
            profile.BackgroundColour,
            profile.DescriptionColourOpacity,
            profile.DescriptionColour,
            profile.OverlayOpacity,
            profile.OverlayColour,
            profile.UsernameColour,
            profile.DiscriminatorColour,
            profile.SidebarItemServerColour,
            profile.SidebarItemGlobalColour,
            profile.SidebarItemBentoColour,
            profile.SidebarItemTimezoneColour,
            profile.SidebarValueServerColour,
            profile.SidebarValueGlobalColour,
            profile.SidebarValueBentoColour,
            profile.SidebarOpacity,
            profile.SidebarColour,
            profile.SidebarBlur,
            profile.FmDivBgopacity,
            profile.FmDivBgcolour,
            profile.FmSongTextOpacity,
            profile.FmSongTextColour,
            profile.FmArtistTextOpacity,
            profile.FmArtistTextColour,
            profile.XpDivBgopacity,
            profile.XpDivBgcolour,
            profile.XpTextOpacity,
            profile.XpTextColour,
            profile.XpText2Opacity,
            profile.XpText2Colour,
            profile.XpDoneServerColour1Opacity,
            profile.XpDoneServerColour1,
            profile.XpDoneServerColour2Opacity,
            profile.XpDoneServerColour2,
            profile.XpDoneServerColour3Opacity,
            profile.XpDoneServerColour3,
            profile.XpDoneGlobalColour1Opacity,
            profile.XpDoneGlobalColour1,
            profile.XpDoneGlobalColour2Opacity,
            profile.XpDoneGlobalColour2,
            profile.XpDoneGlobalColour3Opacity,
            profile.XpDoneGlobalColour3,
            profile.Description,
            profile.Timezone,
            profile.Birthday,
            profile.XpBarOpacity,
            profile.XpBarColour,
            profile.XpBar2Opacity,
            profile.XpBar2Colour
        );
}