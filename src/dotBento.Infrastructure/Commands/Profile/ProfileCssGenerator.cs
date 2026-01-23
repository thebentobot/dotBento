using System.Globalization;
using System.Text;

namespace dotBento.Infrastructure.Commands.Profile;

/// <summary>
/// Generates CSS for profile rendering
/// </summary>
public static class ProfileCssGenerator
{
    public static string Generate(ProfileViewModel viewModel)
    {
        var profile = viewModel.Profile;
        var colors = viewModel.Colors;
        var layout = viewModel.Layout;

        var usernameSize = ProfileStyleHelper.GetUsernameFontSize(viewModel.Username);

        var lastFmSongSize = viewModel.LastFmBoardHtml != null && viewModel.HasLastFmBoard
            ? ProfileStyleHelper.GetLastFmTextFontSize(GetLastFmTrackLength(viewModel.LastFmBoardHtml))
            : ProfileStyleHelper.GetLastFmTextFontSize(4);

        var lastFmArtistSize = viewModel.LastFmBoardHtml != null && viewModel.HasLastFmBoard
            ? ProfileStyleHelper.GetLastFmTextFontSize(GetLastFmArtistLength(viewModel.LastFmBoardHtml))
            : ProfileStyleHelper.GetLastFmTextFontSize(4);

        var serverXpPercent = CalculateXpPercent(viewModel.ServerXp, viewModel.ServerLevel);
        var globalXpPercent = CalculateXpPercent(viewModel.GlobalXp, viewModel.GlobalLevel);

        var css = new StringBuilder();

        css.AppendLine($@":root {{
    --bgimage: url('{profile.BackgroundUrl ?? ""}');
    --user-color: {colors.Background};
}}

body {{
    margin: 0;
    padding: 0;
    font-family: 'Urbanist', sans-serif;
}}

.wrapper {{
    width: 600px;
    height: 400px;
    background-color: var(--user-color);
    overflow: hidden;
    border-radius: 10px;
}}

.custom-bg {{
    background-size: cover;
    background-position: center;
    background-image: var(--bgimage);
}}

.sidebar {{
    position: absolute;
    left: 400px;
    top: 0;
    z-index: 3;
    background-color: {colors.Sidebar};
    width: 200px;
    height: inherit;
    border-radius: 0 10px 10px 0;
    font-family: 'Urbanist', sans-serif;
}}

.blur {{
    overflow: hidden;
    backdrop-filter: blur({profile.SidebarBlur}px);
}}

.avatar {{
    width: 96px;
    height: auto;
}}

.avatar-container {{
    position: absolute;
    overflow: hidden;
    transform: translate(-50%, 16px);
    left: 100px;
    width: 96px;
    height: 96px;
    border-radius: 50%;
    border-width: 0;
    border-style: solid;
    border-color: white;
    z-index: 2;
}}

.sidebar-list {{
    list-style: none;
    text-align: center;
    position: absolute;
    top: 170px;
    right: 0;
    width: 200px;
    color: white;
    line-height: 1.1;
    margin: auto;
    font-family: 'Urbanist', sans-serif;
}}

.sidebar-itemServer {{
    padding-top: 13px;
    height: auto;
    color: {profile.SidebarItemServerColour};
    font-family: 'Urbanist', sans-serif;
}}

.sidebar-itemGlobal {{
    padding-top: 13px;
    height: auto;
    color: {profile.SidebarItemGlobalColour};
    font-family: 'Urbanist', sans-serif;
}}

.sidebar-itemBento {{
    padding-top: 13px;
    height: auto;
    color: {profile.SidebarItemBentoColour};
    font-family: 'Urbanist', sans-serif;
}}

.sidebar-itemTimezone {{
    padding-top: 13px;
    height: auto;
    color: {profile.SidebarItemTimezoneColour};
    font-family: 'Urbanist', sans-serif;
}}

.sidebar-valueServer {{
    font-size: 24px;
    color: {profile.SidebarValueServerColour};
    font-family: 'Urbanist', sans-serif;
}}

.sidebar-valueGlobal {{
    font-size: 24px;
    color: {profile.SidebarValueGlobalColour};
    font-family: 'Urbanist', sans-serif;
}}

.sidebar-valueBento {{
    font-size: 24px;
    color: {profile.SidebarValueBentoColour};
    font-family: 'Urbanist', sans-serif;
}}

.sidebar-valueEmote {{
    font-size: 24px;
    color: #ffffff;
    font-family: 'Urbanist', sans-serif;
}}

.name-container {{
    position: absolute;
    top: 120px;
    width: 200px;
    font-family: 'Urbanist', sans-serif;
}}

.badges {{
    list-style: none;
    padding: 0;
    margin: 10px 10px 5px 20px;
    color: {colors.Description};
}}

.badge-container {{
    display: inline-block;
    margin-right: 0;
}}

.corner-logo {{
    width: 30px;
    height: 30px;
    color: white;
    padding: 3px;
    font-size: 30px;
    z-index: 5;
}}

svg {{
    width: 100%;
    height: 100%;
}}

.username {{
    font-family: 'Urbanist', sans-serif;
    font-size: {usernameSize};
    fill: {profile.UsernameColour};
}}

.discriminator {{
    font-family: 'Urbanist', sans-serif;
    font-size: 17px;
    fill: {profile.DiscriminatorColour};
}}

.footer {{
    position: absolute;
    width: 400px;
    height: 150px;
    top: 250px;
    padding-top: {layout.FmPaddingTop};
    padding-left: 20px;
    border-radius: 0 0 10px 10px;
    display: flex;
    flex-direction: column;
    gap: 8px;
}}

.center-area {{
    position: relative;
    top: 20px;
    width: 325px;
    height: {layout.DescriptionHeight};
    left: 40px;
    margin: 0;
    overflow: hidden;
    color: {colors.Description};
    font-family: 'Urbanist', sans-serif;
}}

.description {{
    font-size: 20px;
    height: auto;
    max-height: 95%;
    width: 300px;
    word-wrap: break-word;
    font-family: 'Urbanist', sans-serif;
    position: absolute;
    bottom: 0;
}}

.description-text {{
    margin: 0;
    font-family: 'Urbanist', sans-serif;
}}

.inner-wrapper {{
    width: inherit;
    height: inherit;
    overflow: hidden;
}}

.xpDivBGBGBG {{
}}

.xpDivBGBGBG2 {{
}}

.xpDivBGBG {{
    position: relative;
}}

.fmDivBGBG {{
    position: relative;
}}

.xpDivBG {{
    flex-grow: 0.5;
    width: 80%;
    overflow: hidden;
    display: flex;
    align-items: center;
    background-color: {colors.XpDivBackground};
    border-radius: 0.5rem;
    padding-left: 25px;
    padding-right: 15px;
    opacity: {layout.XpOpacity};
}}

.xpDivBG2 {{
    flex-grow: 0.5;
    width: 80%;
    overflow: hidden;
    display: flex;
    align-items: center;
    background-color: #ffffff00;
    border-radius: 0.5rem;
    padding-left: 25px;
    padding-right: 15px;
    opacity: {layout.XpOpacity};
}}

.fmDivBG {{
    flex-grow: 0.5;
    width: 80%;
    overflow: hidden;
    display: flex;
    align-items: center;
    background-color: {colors.FmDivBackground};
    border-radius: 0.5rem;
    padding-left: 25px;
    padding-right: 15px;
    opacity: {layout.FmOpacity};
}}

.xpDiv {{
    flex-grow: 1;
    padding: 0.5rem;
    width: 75%;
    overflow: hidden;
}}

.fmDiv {{
    flex-grow: 1;
    padding: 0.5rem;
    width: 75%;
    overflow: hidden;
}}

.xpText {{
    color: {colors.XpText};
    text-align: right;
    overflow: hidden;
    font-family: 'Urbanist', sans-serif;
    font-size: medium;
}}

.xpText2 {{
    color: {colors.XpText2};
    text-align: right;
    overflow: hidden;
    font-family: 'Urbanist', sans-serif;
    font-size: medium;
}}

.fmSongText {{
    --tw-text-opacity: 1;
    color: {colors.FmSongText};
    text-align: left;
    overflow: hidden;
    font-family: 'Urbanist', sans-serif;
    font-size: {lastFmSongSize};
    padding-left: 10px;
    display: block;
    align-items: center;
    position: relative;
    white-space: nowrap;
    text-overflow: ellipsis;
}}

.fmArtistText {{
    --tw-text-opacity: 1;
    color: {colors.FmArtistText};
    text-align: left;
    overflow: hidden;
    font-family: 'Urbanist', sans-serif;
    font-size: {lastFmArtistSize};
    padding-left: 10px;
    display: flex;
    align-items: center;
    position: relative;
    white-space: nowrap;
    text-overflow: ellipsis;
}}

.fmBar {{
    width: 100%;
    --tw-bg-opacity: 1;
    background-color: rgba(55, 65, 81, var(--tw-bg-opacity));
    overflow: hidden;
    opacity: 0%;
}}

.fmDoneServer {{
    background: linear-gradient(to left, #FCD34D, #F59E0B, #EF4444);
    box-shadow: 0 3px 3px -5px #EF4444, 0 2px 5px #EF4444;
    border-radius: 20px;
    color: #fff;
    display: flex;
    align-items: center;
    justify-content: center;
    height: 100%;
    width: 100%;
    opacity: 0%;
}}

.xpBar {{
    margin-top: 0.25rem;
    margin-bottom: 0.25rem;
    width: 100%;
    height: 0.25rem;
    background-color: {colors.XpBar};
    border-radius: 0.25rem;
    overflow: hidden;
}}

.xpBar2 {{
    margin-top: 0.25rem;
    margin-bottom: 0.25rem;
    width: 100%;
    height: 0.25rem;
    background-color: {colors.XpBar2};
    border-radius: 0.25rem;
    overflow: hidden;
}}

.xpDoneServer {{
    background: linear-gradient(to left, {colors.XpDoneServerColor1}, {colors.XpDoneServerColor2}, {colors.XpDoneServerColor3});
    box-shadow: 0 3px 3px -5px #EF4444, 0 2px 5px #EF4444;
    border-radius: 20px;
    color: #fff;
    display: flex;
    align-items: center;
    justify-content: center;
    height: 100%;
    width: {serverXpPercent};
}}

.xpDoneGlobal {{
    background: linear-gradient(to left, {colors.XpDoneGlobalColor1}, {colors.XpDoneGlobalColor2}, {colors.XpDoneGlobalColor3});
    box-shadow: 0 3px 3px -5px #EF4444, 0 2px 5px #EF4444;
    border-radius: 20px;
    color: #fff;
    display: flex;
    align-items: center;
    justify-content: center;
    height: 100%;
    width: {globalXpPercent};
}}

.overlay {{
    background-color: {colors.Overlay};
}}");

        return css.ToString();
    }

    private static string CalculateXpPercent(long xp, int level)
    {
        var percent = Math.Clamp((xp / (Math.Pow(level, 2) * 100)) * 100, 0, 100);
        return percent.ToString("F2", CultureInfo.InvariantCulture) + "%";
    }

    // These are temporary - we'll pass actual lengths when we have the LastFM data structure
    private static int GetLastFmTrackLength(string html) => 4;
    private static int GetLastFmArtistLength(string html) => 4;
}
