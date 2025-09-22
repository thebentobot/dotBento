using System.Globalization;
using CSharpFunctionalExtensions;
using Discord.WebSocket;
using dotBento.Domain.Entities;
using dotBento.Infrastructure.Extensions;
using dotBento.Infrastructure.Services;
using dotBento.Infrastructure.Services.Api;

namespace dotBento.Infrastructure.Commands;

public sealed class ProfileCommands(
    ProfileService profileService,
    SushiiImageServerService sushiiImageServerService,
    LastFmCommands lastFmCommands,
    LastFmService lastFmService,
    UserService userService,
    GuildService guildService,
    BentoService bentoService
)
{
    // TODO: Make this an environment variable
    private const ulong BentoSupportServerId = 714496317522444352;
    private const long DeveloperUserId = 232584569289703424;

    public async Task<Result<Stream>> GetProfileAsync(string imageServerHost, string lastFmApiKey, long userId,
        long guildId, SocketGuildUser guildMember, int guildMemberCount, string botAvatarUrl)
    {
        var profileDb = await profileService.GetProfileAsync(userId);
        var profile = profileDb.HasValue ? MergeWithDefaults(profileDb.Value.Map()) : DefaultProfile(userId);

        var html = GenerateProfileHtml(profile, lastFmApiKey, guildId, guildMember, guildMemberCount, botAvatarUrl);

        var image = await sushiiImageServerService
            .GetSushiiImage(imageServerHost, await html, 600, 400);

        return image.IsSuccess
            ? Result.Success(image.Value)
            : Result.Failure<Stream>("Could not get image from Sushii Image Server");
    }

    // TODO Improve this mess of arguments
    private async Task<string> GenerateProfileHtml(
        Profile profile,
        string lastFmApiKey,
        long guildId,
        SocketGuildUser guildMember,
        int guildUsersAmount,
        string botAvatarUrl)
    {
        var lastFmBoard = profile.LastfmBoard == true ? 
            await GetLastFmNowPlayingHtml(profile, lastFmApiKey) : null;
        var xpBoard = profile.XpBoard == true ? 
            await GetUserXpBoardHtml(profile, guildId, botAvatarUrl) : null;
        // TODO: Make one data method to avoid overfetching and make it more readable
        var bentoUser = userService.GetUserAsync((ulong)profile.UserId).Result.Value;
        var bentoGuildUser = guildService
            .GetOrCreateGuildMemberAsync((ulong)guildId, (ulong)profile.UserId, guildMember).Result.Value;
        var bentoGameData = await bentoService.FindBentoAsync(profile.UserId);
        var bentoUserCount = await userService.GetTotalDiscordUserCountAsync();
        var bentoTotalUserCount = await bentoService.GetTotalCountOfBentoUsersAsync();
        var bentoRank = await bentoService.GetBentoRankAsync(profile.UserId);
        var userGlobalRank = await userService.GetUserRankAsync(profile.UserId);
        var userServerRank = await guildService.GetGuildMemberRankAsync(profile.UserId, guildId);

        var fmOpacity = 100;
        var xpOpacity = 100;
        var descriptionHeight = "250px";
        var fmPaddingTop = "32.5px";

        if (lastFmBoard.HasValue == false && xpBoard.HasValue == false)
        {
            fmOpacity = 0;
            xpOpacity = 0;
            descriptionHeight = "365px";
        }

        if (lastFmBoard.HasValue == false && xpBoard.HasValue)
        {
            fmOpacity = 0;
            descriptionHeight = "310px";
            fmPaddingTop = "88px";
        }

        if (xpBoard.HasValue == false && lastFmBoard.HasValue)
        {
            xpOpacity = 0;
            descriptionHeight = "310px";
            fmPaddingTop = "32.5px";
        }

        // TODO: Improve this mess
        var backgroundColour = $"{profile.BackgroundColour}{ConvertOpacityToHex(profile.BackgroundColourOpacity)}";
        var descriptionColour = $"{profile.DescriptionColour}{ConvertOpacityToHex(profile.DescriptionColourOpacity)}";
        var overlayColour = $"{profile.OverlayColour}{ConvertOpacityToHex(profile.OverlayOpacity)}";
        var sidebarValueEmoteColour = "#ffffff";
        var sidebarColour = $"{profile.SidebarColour}{ConvertOpacityToHex(profile.SidebarOpacity)}";
        var fmDivBGColour = $"{profile.FmDivBgColour}{ConvertOpacityToHex(profile.FmDivBgOpacity)}";
        var fmSongTextColour = $"{profile.FmSongTextColour}{ConvertOpacityToHex(profile.FmSongTextOpacity)}";
        var fmArtistTextColour = $"{profile.FmArtistTextColour}{ConvertOpacityToHex(profile.FmArtistTextOpacity)}";
        var xpDivBGColour = $"{profile.XpDivBgColour}{ConvertOpacityToHex(profile.XpDivBgOpacity)}";
        var xpTextColour = $"{profile.XpTextColour}{ConvertOpacityToHex(profile.XpTextOpacity)}";
        var xpText2Colour = $"{profile.XpText2Colour}{ConvertOpacityToHex(profile.XpText2Opacity)}";
        var xpBarColour = $"{profile.XpBarColour}{ConvertOpacityToHex(profile.XpBarOpacity)}";
        var xpBar2Colour = $"{profile.XpBar2Colour}{ConvertOpacityToHex(profile.XpBar2Opacity)}";
        var xpDoneServerColour1 = $"{profile.XpDoneServerColour1}{ConvertOpacityToHex(profile.XpDoneServerColour1Opacity)}";
        var xpDoneServerColour2 = $"{profile.XpDoneServerColour2}{ConvertOpacityToHex(profile.XpDoneServerColour2Opacity)}";
        var xpDoneServerColour3 = $"{profile.XpDoneServerColour3}{ConvertOpacityToHex(profile.XpDoneServerColour3Opacity)}";
        var xpDoneGlobalColour1 = $"{profile.XpDoneGlobalColour1}{ConvertOpacityToHex(profile.XpDoneGlobalColour1Opacity)}";
        var xpDoneGlobalColour2 = $"{profile.XpDoneGlobalColour2}{ConvertOpacityToHex(profile.XpDoneGlobalColour2Opacity)}";
        var xpDoneGlobalColour3 = $"{profile.XpDoneGlobalColour3}{ConvertOpacityToHex(profile.XpDoneGlobalColour3Opacity)}";

        var avatarUrl = guildMember.GetDisplayAvatarUrl() ??
                        $"https://cdn.discordapp.com/embed/avatars/{int.Parse(guildMember.Discriminator ?? "0") % 5}.png";
        var usernameSlot = guildMember.DisplayName ?? "User";
        var discriminatorSlot = guildMember.Nickname ?? $"#{guildMember.Discriminator}";
        var usernameSize = UsernamePxSize(usernameSlot);
        var xpServer = bentoGuildUser.Xp;
        var xpGlobal = bentoUser.Xp;

        var replacements = new Dictionary<string, string>
        {
            { "BACKGROUND_IMAGE", profile.BackgroundUrl ?? "" },
            { "WRAPPER_CLASS", profile.BackgroundUrl != null ? "custom-bg" : "" },
            { "SIDEBAR_CLASS", profile.BackgroundUrl != null ? "blur" : "" },
            { "OVERLAY_CLASS", profile.BackgroundUrl != null ? "overlay" : "" },
            { "USER_COLOR", backgroundColour },
            { "AVATAR_URL", avatarUrl },
            { "USERNAME", usernameSlot },
            { "DISCRIMINATOR", discriminatorSlot },
            { "DESCRIPTION", profile.Description ?? "I am a happy user of Bento 🍱😄" },
            { "SERVER_LEVEL", userServerRank.ToString() ?? "0" },
            { "GLOBAL_LEVEL", userGlobalRank.ToString() ?? "0" },
            { "USERNAME_SIZE", usernameSize }
        };

        var userTimezone = profile.Timezone != null ? 
            $"{GetCurrentTimeForTimezone(profile.Timezone).ToShortTimeString()} {ShowEmoteAccordingToTimeOfDay(GetCurrentTimeForTimezone(profile.Timezone))} "
            : "";
        var userBirthday = profile.Birthday != null ? 
            DateTime.Parse(profile.Birthday).ToString("MMM d") + " 🎂"
            : "";

        var emoteArray = await GetUserEmotes(profile.UserId);

        var css = $@"
            :root {{
                --bgimage: url('{replacements["BACKGROUND_IMAGE"]}');
                --user-color: {replacements["USER_COLOR"]};
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
                background-color: {sidebarColour};
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
                color: {sidebarValueEmoteColour};
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
                color: {descriptionColour};
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
                font-size: {replacements["USERNAME_SIZE"]};
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
                padding-top: {fmPaddingTop};
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
                height: {descriptionHeight};
                left: 40px;
                margin: 0;
                overflow: hidden;
                color: {descriptionColour};
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
                background-color: {xpDivBGColour};
                border-radius: 0.5rem;
                padding-left: 25px;
                padding-right: 15px;
                opacity: {xpOpacity};
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
                opacity: {xpOpacity};
            }}

            .fmDivBG {{
                flex-grow: 0.5;
                width: 80%;
                overflow: hidden;
                display: flex;
                align-items: center;
                background-color: {fmDivBGColour};
                border-radius: 0.5rem;
                padding-left: 25px;
                padding-right: 15px;
                opacity: {fmOpacity};
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
                color: {xpTextColour};
                text-align: right;
                overflow: hidden;
                font-family: 'Urbanist', sans-serif;
                font-size: medium;
            }}

            .xpText2 {{
                color: {xpText2Colour};
                text-align: right;
                overflow: hidden;
                font-family: 'Urbanist', sans-serif;
                font-size: medium;
            }}

            .fmSongText {{
                --tw-text-opacity: 1;
                color: {fmSongTextColour};
                text-align: left;
                overflow: hidden;
                font-family: 'Urbanist', sans-serif;
                font-size: {LastFmTextPxSize(lastFmBoard.HasValue ? lastFmBoard.Value.LastFmTrackLength : 4)};
                padding-left: 10px;
                display: block;
                align-items: center;
                position: relative;
                white-space: nowrap;
                text-overflow: ellipsis;
            }}

            .fmArtistText {{
                --tw-text-opacity: 1;
                color: {fmArtistTextColour};
                text-align: left;
                overflow: hidden;
                font-family: 'Urbanist', sans-serif;
                font-size: {LastFmTextPxSize(lastFmBoard.HasValue ? lastFmBoard.Value.LastFmArtistLength : 4)};
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
                background-color: {xpBarColour};
                border-radius: 0.25rem;
                overflow: hidden;
            }}

            .xpBar2 {{
                margin-top: 0.25rem;
                margin-bottom: 0.25rem;
                width: 100%;
                height: 0.25rem;
                background-color: {xpBar2Colour};
                border-radius: 0.25rem;
                overflow: hidden;
            }}

            .xpDoneServer {{
                background: linear-gradient(to left, {xpDoneServerColour1}, {xpDoneServerColour2}, {xpDoneServerColour3});
                box-shadow: 0 3px 3px -5px #EF4444, 0 2px 5px #EF4444;
                border-radius: 20px;
                color: #fff;
                display: flex;
                align-items: center;
                justify-content: center;
                height: 100%;
                width: {Math.Clamp((xpServer / (Math.Pow(bentoGuildUser.Level, 2) * 100)) * 100, 0, 100).ToString("F2", CultureInfo.InvariantCulture)}%;
            }}

            .xpDoneGlobal {{
                background: linear-gradient(to left, {xpDoneGlobalColour1}, {xpDoneGlobalColour2}, {xpDoneGlobalColour3});
                box-shadow: 0 3px 3px -5px #EF4444, 0 2px 5px #EF4444;
                border-radius: 20px;
                color: #fff;
                display: flex;
                align-items: center;
                justify-content: center;
                height: 100%;
                width: {Math.Clamp((xpGlobal / (Math.Pow(bentoUser.Level, 2) * 100)) * 100, 0, 100).ToString("F2", CultureInfo.InvariantCulture)}%;
            }}

            .overlay {{
                background-color: {overlayColour};
            }}
        ";
        
        var htmlString = $@"
            <div class='wrapper {replacements["WRAPPER_CLASS"]}'>
                <div class='inner-wrapper {replacements["OVERLAY_CLASS"]}'>
            
                    <div class='center-area'>
                        <div class='description'>
                            <p class='description-text'>{replacements["DESCRIPTION"]}</p>
                        </div>
                    </div>
            
                    <div class='sidebar {replacements["SIDEBAR_CLASS"]}'>
            
                        <div class='avatar-container'>
                            <img class='avatar' src='{replacements["AVATAR_URL"]}'>
                        </div>
            
                        <div class='name-container'>
                            <svg width='200' height='50'>
                                <text class='username' x='50%' y='30%' dominant-baseline='middle' text-anchor='middle'>
                                    {replacements["USERNAME"]}
                                </text>
                                <text class='discriminator' x='50%' y='75%' dominant-baseline='middle' text-anchor='middle'>
                                    {replacements["DISCRIMINATOR"]}
                                </text>
                            </svg>
                        </div>
            
                        <ul class='sidebar-list'>
                            <li class='sidebar-itemServer'>
                                <span class='sidebar-valueServer'>Rank {replacements["SERVER_LEVEL"]}</span><br>
                                Of {guildUsersAmount} Users
                            </li>
                            <li class='sidebar-itemGlobal'>
                                <span class='sidebar-valueGlobal'>Rank {replacements["GLOBAL_LEVEL"]}</span><br>
                                Of {Math.Floor(bentoUserCount.Value / 100.0) / 10.0:F1}k Users
                            </li>
                            {(bentoGameData.HasValue
                                ? $"<li class='sidebar-itemBento'><span class='sidebar-valueBento'>{bentoGameData.Value.Bento1} 🍱</span><br>Rank {bentoRank.Value}/{bentoTotalUserCount} 🍱 Users</li>"
                                : string.Empty)}
                            <li class='sidebar-itemTimezone'>
                                <span class='sidebar-valueEmote'>{string.Join("", emoteArray)}</span><br>
                                {userTimezone} {userBirthday}
                            </li>
                        </ul>
            
                    </div>
                    <div class='footer'>
                        {(lastFmBoard.HasValue ? lastFmBoard.Value.LastFmHtml : null)}
                        {(xpBoard.HasValue ? xpBoard.Value : null)}
                    </div>
                </div>
            </div>";

        htmlString = $@"
            <html>
            <head>
                <link href='https://fonts.googleapis.com/css2?family=Urbanist:wght@400;700&display=swap' rel='stylesheet'>
                <meta charset='UTF-8'>
            </head>
            <style>
                {css}
            </style>
            <body>
                {htmlString}
            </body>
            </html>";
        
        return htmlString;
    }

    private async Task<Maybe<LastFmHtmlBoardResult>> GetLastFmNowPlayingHtml(Profile profile, string lastFmApiKey)
    {
        var lastfmProfile = await lastFmService.GetLastFmAsync(profile.UserId);

        if (!lastfmProfile.HasValue) return Maybe<LastFmHtmlBoardResult>.None;
        var lastfmNowPlaying =
            await lastFmCommands.NowPlaying(lastfmProfile.Value.Lastfm1, lastFmApiKey);

        if (lastfmNowPlaying.Value == null) return Maybe<LastFmHtmlBoardResult>.None;
        var latestSong = lastfmNowPlaying.Value.RecentTracks[0];

        var lastFmHtml = $@"
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
        
        return new LastFmHtmlBoardResult(lastFmHtml, latestSong.Track.Length, latestSong.Artist.Length);
    }
    
    private static DateTime GetCurrentTimeForTimezone(string timezone)
    {
        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timezone);
        var time = DateTime.UtcNow;
        return TimeZoneInfo.ConvertTimeFromUtc(time, timeZoneInfo);
    }

    private async Task<Maybe<string>> GetUserXpBoardHtml(Profile profile, long guildId, string botAvatarUrl)
    {
        var user = await userService.GetUserAsync((ulong)profile.UserId);
        if (user.HasNoValue) return Maybe<string>.None;
        var guild = await guildService.GetGuildAsync((ulong)guildId);
        if (guild.HasNoValue) return Maybe<string>.None;
        var guildMember = await guildService.GetGuildMemberAsync((ulong)guildId, (ulong)profile.UserId);
        if (guildMember.HasNoValue) return Maybe<string>.None;

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

    private static string UsernamePxSize(string username) =>
        username.Length switch
        {
            <= 15 => "24px",
            <= 20 => "18px",
            <= 25 => "15px",
            _ => "11px"
        };

    private static string LastFmTextPxSize(int textLength) =>
        textLength switch
        {
            <= 36 => "16px",
            <= 40 => "14px",
            <= 46 => "12px",
            _ => "10px"
        };

    private static string GetEmote(string emote) => $"<img src=\"{emote}\" width=\"24\" height=\"24\">";

    private static Profile DefaultProfile(long userId) => new(
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

    private static Profile MergeWithDefaults(Profile dbProfile)
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
            XpDoneServerColour1Opacity =
            dbProfile.XpDoneServerColour1Opacity ?? defaultProfile.XpDoneServerColour1Opacity,
            XpDoneServerColour1 = dbProfile.XpDoneServerColour1 ?? defaultProfile.XpDoneServerColour1,
            XpDoneServerColour2Opacity =
            dbProfile.XpDoneServerColour2Opacity ?? defaultProfile.XpDoneServerColour2Opacity,
            XpDoneServerColour2 = dbProfile.XpDoneServerColour2 ?? defaultProfile.XpDoneServerColour2,
            XpDoneServerColour3Opacity =
            dbProfile.XpDoneServerColour3Opacity ?? defaultProfile.XpDoneServerColour3Opacity,
            XpDoneServerColour3 = dbProfile.XpDoneServerColour3 ?? defaultProfile.XpDoneServerColour3,
            XpDoneGlobalColour1Opacity =
            dbProfile.XpDoneGlobalColour1Opacity ?? defaultProfile.XpDoneGlobalColour1Opacity,
            XpDoneGlobalColour1 = dbProfile.XpDoneGlobalColour1 ?? defaultProfile.XpDoneGlobalColour1,
            XpDoneGlobalColour2Opacity =
            dbProfile.XpDoneGlobalColour2Opacity ?? defaultProfile.XpDoneGlobalColour2Opacity,
            XpDoneGlobalColour2 = dbProfile.XpDoneGlobalColour2 ?? defaultProfile.XpDoneGlobalColour2,
            XpDoneGlobalColour3Opacity =
            dbProfile.XpDoneGlobalColour3Opacity ?? defaultProfile.XpDoneGlobalColour3Opacity,
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

    private async Task<string[]> GetUserEmotes(long userId)
    {
        var emotes = new List<string> { GenerateRandomEmote() };

        await AddBentoSupportServerEmote(userId, emotes);
        AddDeveloperEmote(userId, emotes);
        await AddPatreonEmotes(userId, emotes);

        return emotes.ToArray();
    }

    private async Task AddBentoSupportServerEmote(long userId, List<string> emotes)
    {
        var guildMember = await guildService.GetGuildMemberAsync(BentoSupportServerId, (ulong)userId);
        if (guildMember.HasValue)
        {
            emotes.Add("🍱");
        }
    }

    private void AddDeveloperEmote(long userId, List<string> emotes)
    {
        if (userId == DeveloperUserId)
        {
            emotes.Add("👨‍💻");
        }
    }

    private async Task AddPatreonEmotes(long userId, List<string> emotes)
    {
        var patreon = await userService.GetPatreonUserAsync((ulong)userId);
        if (patreon.HasValue)
        {
            var patreonUser = patreon.Value;
            if (patreonUser.EmoteSlot1 != null)
            {
                emotes.Add(GetEmote(patreonUser.EmoteSlot1));
            }

            if (patreonUser.EmoteSlot2 != null &&
                (patreonUser.Enthusiast || patreonUser.Disciple || patreonUser.Sponsor))
            {
                emotes.Add(GetEmote(patreonUser.EmoteSlot2));
            }

            if (patreonUser.EmoteSlot3 != null && (patreonUser.Disciple || patreonUser.Sponsor))
            {
                emotes.Add(GetEmote(patreonUser.EmoteSlot3));
            }

            if (patreonUser is { EmoteSlot4: not null, Sponsor: true })
            {
                emotes.Add(GetEmote(patreonUser.EmoteSlot4));
            }
        }
    }

    private static string GenerateRandomEmote() => RandomEmotes[new Random().Next(RandomEmotes.Length)];

    private static string[] RandomEmotes =>
    [
        "😀",
        "😃",
        "😄",
        "😁",
        "😆",
        "😅",
        "😂",
        "🤣",
        "😊",
        "😇",
        "😉",
        "😍",
        "🥰",
        "😘",
        "😗",
        "😙",
        "😚",
        "😋",
        "😛",
        "😝",
        "😜",
        "🤪",
        "🧐",
        "🤓",
        "😎",
        "🤩",
        "🥳",
        "😏",
        "😫",
        "😩",
        "🥺",
        "😭",
        "😤",
        "😳",
        "🥵",
        "🥶",
        "😱",
        "😨",
        "🤗",
        "🤔",
        "🤭",
        "🙄",
        "😲",
        "🤤",
        "🥴",
        "🤑",
        "🤠",
        "😈",
        "👿",
        "🤡",
    ];
    
    private static string ShowEmoteAccordingToTimeOfDay(DateTime timeOfDay)
    {
        var hour = timeOfDay.Hour;
        return hour switch
        {
            < 4 => "🌌",
            < 8 => "🌅",
            < 12 => "☀️",
            < 16 => "🌞",
            < 20 => "🌇",
            _ => "🌙"
        };
    }
    
    private static string ConvertOpacityToHex(int? opacityPercentage)
    {
        int opacityValue = opacityPercentage ?? 100; // Default to fully opaque if null
        int hexValue = (int)(opacityValue / 100.0 * 255);
        return hexValue.ToString("X2"); // Convert to a 2-character hex string
    }
}

public sealed record LastFmHtmlBoardResult(string LastFmHtml, int LastFmTrackLength, int LastFmArtistLength);