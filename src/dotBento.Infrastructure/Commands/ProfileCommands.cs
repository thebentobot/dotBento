using CSharpFunctionalExtensions;
using Discord.WebSocket;
using dotBento.Domain.Entities;
using dotBento.Infrastructure.Commands.Profile;
using dotBento.Infrastructure.Extensions;
using dotBento.Infrastructure.Services;
using dotBento.Infrastructure.Services.Api;

namespace dotBento.Infrastructure.Commands;

/// <summary>
/// Commands for generating user profile images
/// </summary>
public sealed class ProfileCommands(
    ProfileService profileService,
    SushiiImageServerService sushiiImageServerService,
    LastFmCommands lastFmCommands,
    LastFmService lastFmService,
    UserService userService,
    GuildService guildService,
    BentoService bentoService)
{
    private readonly ProfileViewModelBuilder _viewModelBuilder = new(userService, guildService, bentoService);

    public async Task<Result<Stream>> GetProfileAsync(
        string imageServerHost,
        string lastFmApiKey,
        long userId,
        long guildId,
        SocketGuildUser guildMember,
        int guildMemberCount,
        string botAvatarUrl)
    {
        // Get profile from database or use defaults
        var profileDb = await profileService.GetProfileAsync(userId);
        var profile = profileDb.HasValue
            ? MergeWithDefaults(profileDb.Value.Map())
            : DefaultProfile(userId);

        // Generate board HTML if enabled
        var lastFmBoardHtml = profile.LastfmBoard == true
            ? await GetLastFmNowPlayingHtmlAsync(profile, lastFmApiKey)
            : null;

        var xpBoardHtml = profile.XpBoard == true
            ? await GetUserXpBoardHtmlAsync(profile, guildId, botAvatarUrl)
            : null;

        // Build the view model with all necessary data
        var viewModelResult = await _viewModelBuilder.BuildAsync(
            profile,
            guildId,
            guildMember,
            guildMemberCount,
            botAvatarUrl,
            lastFmBoardHtml,
            xpBoardHtml);

        if (viewModelResult.IsFailure)
        {
            return Result.Failure<Stream>(viewModelResult.Error);
        }

        var viewModel = viewModelResult.Value;

        // Generate CSS and HTML
        var css = ProfileCssGenerator.Generate(viewModel);
        var html = ProfileHtmlGenerator.Generate(viewModel, css);

        // Render to image
        var image = await sushiiImageServerService.GetSushiiImage(imageServerHost, html, 600, 400);

        return image.IsSuccess
            ? Result.Success(image.Value)
            : Result.Failure<Stream>("Could not get image from Sushii Image Server");
    }

    private async Task<string?> GetLastFmNowPlayingHtmlAsync(Domain.Entities.Profile profile, string lastFmApiKey)
    {
        var lastfmProfile = await lastFmService.GetLastFmAsync(profile.UserId);
        if (!lastfmProfile.HasValue) return null;

        var lastfmNowPlaying = await lastFmCommands.NowPlaying(lastfmProfile.Value.Lastfm1, lastFmApiKey);
        if (lastfmNowPlaying.IsFailure || lastfmNowPlaying.Value == null) return null;

        var latestSong = lastfmNowPlaying.Value.RecentTracks[0];

        return $@"
            <div class='xpDivBGBGBG2'>
                <div class='fmDivBGBG'>
                    <div class='fmDivBG'>
                        <div class='fmDiv'>
                            <img src='{latestSong.Image}' width='36' height='36' style='float:left'>
                            <div>
                                <div class='fmSongText'>
                                    {latestSong.Track}
                                </div>
                                <div class='fmArtistText'>
                                    {latestSong.Artist}
                                </div>
                            </div>
                            <div class='fmBar'>
                                <div class='fmDoneServer'></div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>";
    }

    private async Task<string?> GetUserXpBoardHtmlAsync(Domain.Entities.Profile profile, long guildId, string botAvatarUrl)
    {
        var user = await userService.GetUserAsync((ulong)profile.UserId);
        if (user.HasNoValue) return null;

        var guild = await guildService.GetGuildAsync((ulong)guildId);
        if (guild.HasNoValue) return null;

        var guildMember = await guildService.GetGuildMemberAsync((ulong)guildId, (ulong)profile.UserId);
        if (guildMember.HasNoValue) return null;

        return $@"
            <div class='xpDivBGBGBG'>
                <div class='xpDivBGBG'>
                    <div class='xpDivBG'>
                        <div class='xpDiv'>
                            <div class='xpText'>
                                {(guild.Value.Icon != "🏠" ? $"<img src='{guild.Value.Icon}' width='20' height='20'>" : "🏠")}
                                Level {guildMember.Value.Level}
                            </div>
                            <div class='xpBar'>
                                <div class='xpDoneServer'></div>
                            </div>
                        </div>
                        <div class='xpDiv'>
                            <div class='xpText2'>
                                <img src='{botAvatarUrl}' width='20' height='20'> Level {user.Value.Level}
                            </div>
                            <div class='xpBar2'>
                                <div class='xpDoneGlobal'></div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>";
    }

    private static Domain.Entities.Profile DefaultProfile(long userId) => new(
        UserId: userId,
        LastfmBoard: false,
        XpBoard: true,
        BackgroundUrl: null,
        BackgroundColourOpacity: 100,
        BackgroundColour: "#1F2937",
        DescriptionColourOpacity: 100,
        DescriptionColour: "#ffffff",
        OverlayOpacity: 20,
        OverlayColour: "#000000",
        UsernameColour: "#ffffff",
        DiscriminatorColour: "#9CA3AF",
        SidebarItemServerColour: "#D3D3D3",
        SidebarItemGlobalColour: "#D3D3D3",
        SidebarItemBentoColour: "#D3D3D3",
        SidebarItemTimezoneColour: "#D3D3D3",
        SidebarValueServerColour: "#ffffff",
        SidebarValueGlobalColour: "#ffffff",
        SidebarValueBentoColour: "#ffffff",
        SidebarOpacity: 30,
        SidebarColour: "#000000",
        SidebarBlur: 3,
        FmDivBgOpacity: 100,
        FmDivBgColour: "#111827",
        FmSongTextOpacity: 100,
        FmSongTextColour: "#ffffff",
        FmArtistTextOpacity: 100,
        FmArtistTextColour: "#ffffff",
        XpDivBgOpacity: 100,
        XpDivBgColour: "#111827",
        XpTextOpacity: 100,
        XpTextColour: "#ffffff",
        XpText2Opacity: 100,
        XpText2Colour: "#ffffff",
        XpDoneServerColour1Opacity: 100,
        XpDoneServerColour1: "#FCD34D",
        XpDoneServerColour2Opacity: 100,
        XpDoneServerColour2: "#F59E0B",
        XpDoneServerColour3Opacity: 100,
        XpDoneServerColour3: "#EF4444",
        XpDoneGlobalColour1Opacity: 100,
        XpDoneGlobalColour1: "#FCD34D",
        XpDoneGlobalColour2Opacity: 100,
        XpDoneGlobalColour2: "#F59E0B",
        XpDoneGlobalColour3Opacity: 100,
        XpDoneGlobalColour3: "#EF4444",
        Description: "I am a happy user of Bento 🍱😄",
        Timezone: null,
        Birthday: null,
        XpBarOpacity: 100,
        XpBarColour: "#374151",
        XpBar2Opacity: 100,
        XpBar2Colour: "#374151"
    );

    private static Domain.Entities.Profile MergeWithDefaults(Domain.Entities.Profile dbProfile)
    {
        var defaultProfile = DefaultProfile(dbProfile.UserId);
        return dbProfile with
        {
            LastfmBoard = dbProfile.LastfmBoard ?? defaultProfile.LastfmBoard,
            XpBoard = dbProfile.XpBoard ?? defaultProfile.XpBoard,
            BackgroundUrl = dbProfile.BackgroundUrl ?? defaultProfile.BackgroundUrl,
            BackgroundColourOpacity = dbProfile.BackgroundColourOpacity ?? defaultProfile.BackgroundColourOpacity,
            BackgroundColour = dbProfile.BackgroundColour ?? defaultProfile.BackgroundColour,
            DescriptionColourOpacity = dbProfile.DescriptionColourOpacity ?? defaultProfile.DescriptionColourOpacity,
            DescriptionColour = dbProfile.DescriptionColour ?? defaultProfile.DescriptionColour,
            OverlayOpacity = dbProfile.OverlayOpacity ?? defaultProfile.OverlayOpacity,
            OverlayColour = dbProfile.OverlayColour ?? defaultProfile.OverlayColour,
            UsernameColour = dbProfile.UsernameColour ?? defaultProfile.UsernameColour,
            DiscriminatorColour = dbProfile.DiscriminatorColour ?? defaultProfile.DiscriminatorColour,
            SidebarItemServerColour = dbProfile.SidebarItemServerColour ?? defaultProfile.SidebarItemServerColour,
            SidebarItemGlobalColour = dbProfile.SidebarItemGlobalColour ?? defaultProfile.SidebarItemGlobalColour,
            SidebarItemBentoColour = dbProfile.SidebarItemBentoColour ?? defaultProfile.SidebarItemBentoColour,
            SidebarItemTimezoneColour = dbProfile.SidebarItemTimezoneColour ?? defaultProfile.SidebarItemTimezoneColour,
            SidebarValueServerColour = dbProfile.SidebarValueServerColour ?? defaultProfile.SidebarValueServerColour,
            SidebarValueGlobalColour = dbProfile.SidebarValueGlobalColour ?? defaultProfile.SidebarValueGlobalColour,
            SidebarValueBentoColour = dbProfile.SidebarValueBentoColour ?? defaultProfile.SidebarValueBentoColour,
            SidebarOpacity = dbProfile.SidebarOpacity ?? defaultProfile.SidebarOpacity,
            SidebarColour = dbProfile.SidebarColour ?? defaultProfile.SidebarColour,
            SidebarBlur = dbProfile.SidebarBlur ?? defaultProfile.SidebarBlur,
            FmDivBgOpacity = dbProfile.FmDivBgOpacity ?? defaultProfile.FmDivBgOpacity,
            FmDivBgColour = dbProfile.FmDivBgColour ?? defaultProfile.FmDivBgColour,
            FmSongTextOpacity = dbProfile.FmSongTextOpacity ?? defaultProfile.FmSongTextOpacity,
            FmSongTextColour = dbProfile.FmSongTextColour ?? defaultProfile.FmSongTextColour,
            FmArtistTextOpacity = dbProfile.FmArtistTextOpacity ?? defaultProfile.FmArtistTextOpacity,
            FmArtistTextColour = dbProfile.FmArtistTextColour ?? defaultProfile.FmArtistTextColour,
            XpDivBgOpacity = dbProfile.XpDivBgOpacity ?? defaultProfile.XpDivBgOpacity,
            XpDivBgColour = dbProfile.XpDivBgColour ?? defaultProfile.XpDivBgColour,
            XpTextOpacity = dbProfile.XpTextOpacity ?? defaultProfile.XpTextOpacity,
            XpTextColour = dbProfile.XpTextColour ?? defaultProfile.XpTextColour,
            XpText2Opacity = dbProfile.XpText2Opacity ?? defaultProfile.XpText2Opacity,
            XpText2Colour = dbProfile.XpText2Colour ?? defaultProfile.XpText2Colour,
            XpDoneServerColour1Opacity = dbProfile.XpDoneServerColour1Opacity ?? defaultProfile.XpDoneServerColour1Opacity,
            XpDoneServerColour1 = dbProfile.XpDoneServerColour1 ?? defaultProfile.XpDoneServerColour1,
            XpDoneServerColour2Opacity = dbProfile.XpDoneServerColour2Opacity ?? defaultProfile.XpDoneServerColour2Opacity,
            XpDoneServerColour2 = dbProfile.XpDoneServerColour2 ?? defaultProfile.XpDoneServerColour2,
            XpDoneServerColour3Opacity = dbProfile.XpDoneServerColour3Opacity ?? defaultProfile.XpDoneServerColour3Opacity,
            XpDoneServerColour3 = dbProfile.XpDoneServerColour3 ?? defaultProfile.XpDoneServerColour3,
            XpDoneGlobalColour1Opacity = dbProfile.XpDoneGlobalColour1Opacity ?? defaultProfile.XpDoneGlobalColour1Opacity,
            XpDoneGlobalColour1 = dbProfile.XpDoneGlobalColour1 ?? defaultProfile.XpDoneGlobalColour1,
            XpDoneGlobalColour2Opacity = dbProfile.XpDoneGlobalColour2Opacity ?? defaultProfile.XpDoneGlobalColour2Opacity,
            XpDoneGlobalColour2 = dbProfile.XpDoneGlobalColour2 ?? defaultProfile.XpDoneGlobalColour2,
            XpDoneGlobalColour3Opacity = dbProfile.XpDoneGlobalColour3Opacity ?? defaultProfile.XpDoneGlobalColour3Opacity,
            XpDoneGlobalColour3 = dbProfile.XpDoneGlobalColour3 ?? defaultProfile.XpDoneGlobalColour3,
            Description = dbProfile.Description ?? defaultProfile.Description,
            Timezone = dbProfile.Timezone ?? defaultProfile.Timezone,
            Birthday = dbProfile.Birthday ?? defaultProfile.Birthday,
            XpBarOpacity = dbProfile.XpBarOpacity ?? defaultProfile.XpBarOpacity,
            XpBarColour = dbProfile.XpBarColour ?? defaultProfile.XpBarColour,
            XpBar2Opacity = dbProfile.XpBar2Opacity ?? defaultProfile.XpBar2Opacity,
            XpBar2Colour = dbProfile.XpBar2Colour ?? defaultProfile.XpBar2Colour
        };
    }
}
