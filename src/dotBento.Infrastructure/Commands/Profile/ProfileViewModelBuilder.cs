using System.Globalization;
using CSharpFunctionalExtensions;
using Discord.WebSocket;
using dotBento.Domain.Entities;
using dotBento.Infrastructure.Services;

namespace dotBento.Infrastructure.Commands.Profile;

/// <summary>
/// Builds a ProfileViewModel by gathering data from various services
/// </summary>
public sealed class ProfileViewModelBuilder(
    UserService userService,
    GuildService guildService,
    BentoService bentoService)
{
    private const ulong BentoSupportServerId = 714496317522444352;
    private const long DeveloperUserId = 232584569289703424;

    public async Task<Result<ProfileViewModel>> BuildAsync(
        Domain.Entities.Profile profile,
        long guildId,
        SocketGuildUser guildMember,
        int guildUserCount,
        string botAvatarUrl,
        string? lastFmBoardHtml,
        string? xpBoardHtml)
    {
        // Fetch user and guild member data
        var userMaybe = await userService.GetUserAsync((ulong)profile.UserId);
        if (userMaybe.HasNoValue)
        {
            return Result.Failure<ProfileViewModel>($"User {profile.UserId} not found in database");
        }
        var user = userMaybe.Value;

        var guildMemberResult = await guildService
            .GetOrCreateGuildMemberAsync((ulong)guildId, (ulong)profile.UserId, guildMember);
        if (guildMemberResult.HasNoValue)
        {
            return Result.Failure<ProfileViewModel>(
                $"Failed to get or create guild member for user {profile.UserId} in guild {guildId}");
        }
        var dbGuildMember = guildMemberResult.Value;

        // Fetch statistics
        var bentoGameData = await bentoService.FindBentoAsync(profile.UserId);
        var bentoUserCount = await userService.GetTotalDiscordUserCountAsync();
        var bentoTotalUserCount = await bentoService.GetTotalCountOfBentoUsersAsync();
        var bentoRank = await bentoService.GetBentoRankAsync(profile.UserId);
        var userGlobalRank = await userService.GetUserRankAsync(profile.UserId);
        var userServerRank = await guildService.GetGuildMemberRankAsync(profile.UserId, guildId);

        // Calculate layout
        var hasLastFmBoard = lastFmBoardHtml != null;
        var hasXpBoard = xpBoardHtml != null;
        var layout = ProfileLayoutCalculator.CalculateLayout(hasLastFmBoard, hasXpBoard);

        // Build colors
        var colors = BuildColors(profile);

        // Build display strings
        var avatarUrl = guildMember.GetDisplayAvatarUrl() ??
                        $"https://cdn.discordapp.com/embed/avatars/{int.Parse(guildMember.Discriminator ?? "0") % 5}.png";
        var username = guildMember.DisplayName ?? "User";
        var discriminator = guildMember.Nickname ?? $"#{guildMember.Discriminator}";
        var description = profile.Description ?? "I am a happy user of Bento 🍱😄";

        var timezoneDisplay = profile.Timezone != null
            ? $"{GetCurrentTimeForTimezone(profile.Timezone).ToShortTimeString()} {ShowEmoteAccordingToTimeOfDay(GetCurrentTimeForTimezone(profile.Timezone))} "
            : "";

        var birthdayDisplay = FormatBirthday(profile.Birthday);
        var emotes = await GetUserEmotesAsync(profile.UserId);

        return new ProfileViewModel
        {
            Profile = profile,
            User = user,
            GuildMember = dbGuildMember,
            AvatarUrl = avatarUrl,
            Username = username,
            Discriminator = discriminator,
            Description = description,
            ServerRank = userServerRank.HasValue ? (long)userServerRank.Value : 0,
            GlobalRank = userGlobalRank.HasValue ? (long)userGlobalRank.Value : 0,
            ServerLevel = dbGuildMember.Level,
            GlobalLevel = user.Level,
            ServerXp = dbGuildMember.Xp,
            GlobalXp = user.Xp,
            HasBentoData = bentoGameData.HasValue,
            BentoCount = bentoGameData.HasValue ? bentoGameData.Value.Bento1 : 0,
            BentoRank = bentoRank.HasValue ? (long)bentoRank.Value : 0,
            TotalBentoUsers = bentoTotalUserCount,
            TotalUserCount = bentoUserCount.HasValue ? Math.Floor(bentoUserCount.Value / 100.0) / 10.0 : 0,
            GuildUserCount = guildUserCount,
            TimezoneDisplay = timezoneDisplay,
            BirthdayDisplay = birthdayDisplay,
            Emotes = emotes,
            LastFmBoardHtml = lastFmBoardHtml,
            XpBoardHtml = xpBoardHtml,
            HasLastFmBoard = hasLastFmBoard,
            HasXpBoard = hasXpBoard,
            Layout = layout,
            Colors = colors
        };
    }

    private static ProfileColors BuildColors(Domain.Entities.Profile profile)
    {
        return new ProfileColors
        {
            Background = ProfileStyleHelper.ToColorWithOpacity(profile.BackgroundColour, profile.BackgroundColourOpacity),
            Description = ProfileStyleHelper.ToColorWithOpacity(profile.DescriptionColour, profile.DescriptionColourOpacity),
            Overlay = ProfileStyleHelper.ToColorWithOpacity(profile.OverlayColour, profile.OverlayOpacity),
            Sidebar = ProfileStyleHelper.ToColorWithOpacity(profile.SidebarColour, profile.SidebarOpacity),
            FmDivBackground = ProfileStyleHelper.ToColorWithOpacity(profile.FmDivBgColour, profile.FmDivBgOpacity),
            FmSongText = ProfileStyleHelper.ToColorWithOpacity(profile.FmSongTextColour, profile.FmSongTextOpacity),
            FmArtistText = ProfileStyleHelper.ToColorWithOpacity(profile.FmArtistTextColour, profile.FmArtistTextOpacity),
            XpDivBackground = ProfileStyleHelper.ToColorWithOpacity(profile.XpDivBgColour, profile.XpDivBgOpacity),
            XpText = ProfileStyleHelper.ToColorWithOpacity(profile.XpTextColour, profile.XpTextOpacity),
            XpText2 = ProfileStyleHelper.ToColorWithOpacity(profile.XpText2Colour, profile.XpText2Opacity),
            XpBar = ProfileStyleHelper.ToColorWithOpacity(profile.XpBarColour, profile.XpBarOpacity),
            XpBar2 = ProfileStyleHelper.ToColorWithOpacity(profile.XpBar2Colour, profile.XpBar2Opacity),
            XpDoneServerColor1 = ProfileStyleHelper.ToColorWithOpacity(profile.XpDoneServerColour1, profile.XpDoneServerColour1Opacity),
            XpDoneServerColor2 = ProfileStyleHelper.ToColorWithOpacity(profile.XpDoneServerColour2, profile.XpDoneServerColour2Opacity),
            XpDoneServerColor3 = ProfileStyleHelper.ToColorWithOpacity(profile.XpDoneServerColour3, profile.XpDoneServerColour3Opacity),
            XpDoneGlobalColor1 = ProfileStyleHelper.ToColorWithOpacity(profile.XpDoneGlobalColour1, profile.XpDoneGlobalColour1Opacity),
            XpDoneGlobalColor2 = ProfileStyleHelper.ToColorWithOpacity(profile.XpDoneGlobalColour2, profile.XpDoneGlobalColour2Opacity),
            XpDoneGlobalColor3 = ProfileStyleHelper.ToColorWithOpacity(profile.XpDoneGlobalColour3, profile.XpDoneGlobalColour3Opacity)
        };
    }

    private async Task<string[]> GetUserEmotesAsync(long userId)
    {
        var emotes = new List<string> { GenerateRandomEmote() };

        await AddBentoSupportServerEmoteAsync(userId, emotes);
        AddDeveloperEmote(userId, emotes);
        await AddPatreonEmotesAsync(userId, emotes);

        return emotes.ToArray();
    }

    private async Task AddBentoSupportServerEmoteAsync(long userId, List<string> emotes)
    {
        var guildMember = await guildService.GetGuildMemberAsync(BentoSupportServerId, (ulong)userId);
        if (guildMember.HasValue)
        {
            emotes.Add("🍱");
        }
    }

    private static void AddDeveloperEmote(long userId, List<string> emotes)
    {
        if (userId == DeveloperUserId)
        {
            emotes.Add("👨‍💻");
        }
    }

    private async Task AddPatreonEmotesAsync(long userId, List<string> emotes)
    {
        var patreon = await userService.GetPatreonUserAsync((ulong)userId);
        if (!patreon.HasValue) return;

        var patreonUser = patreon.Value;
        if (patreonUser.EmoteSlot1 != null)
        {
            emotes.Add(ProfileStyleHelper.GetEmoteHtml(patreonUser.EmoteSlot1));
        }

        if (patreonUser.EmoteSlot2 != null &&
            (patreonUser.Enthusiast || patreonUser.Disciple || patreonUser.Sponsor))
        {
            emotes.Add(ProfileStyleHelper.GetEmoteHtml(patreonUser.EmoteSlot2));
        }

        if (patreonUser.EmoteSlot3 != null && (patreonUser.Disciple || patreonUser.Sponsor))
        {
            emotes.Add(ProfileStyleHelper.GetEmoteHtml(patreonUser.EmoteSlot3));
        }

        if (patreonUser is { EmoteSlot4: not null, Sponsor: true })
        {
            emotes.Add(ProfileStyleHelper.GetEmoteHtml(patreonUser.EmoteSlot4));
        }
    }

    private static string GenerateRandomEmote()
    {
        var randomEmotes = new[]
        {
            "😀", "😃", "😄", "😁", "😆", "😅", "😂", "🤣", "😊", "😇",
            "😉", "😍", "🥰", "😘", "😗", "😙", "😚", "😋", "😛", "😝",
            "😜", "🤪", "🧐", "🤓", "😎", "🤩", "🥳", "😏", "😫", "😩",
            "🥺", "😭", "😤", "😳", "🥵", "🥶", "😱", "😨", "🤗", "🤔",
            "🤭", "🙄", "😲", "🤤", "🥴", "🤑", "🤠", "😈", "👿", "🤡"
        };
        return randomEmotes[Random.Shared.Next(randomEmotes.Length)];
    }

    private static DateTime GetCurrentTimeForTimezone(string timezone)
    {
        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timezone);
        var time = DateTime.UtcNow;
        return TimeZoneInfo.ConvertTimeFromUtc(time, timeZoneInfo);
    }

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

    private static string FormatBirthday(string? birthday)
    {
        if (string.IsNullOrWhiteSpace(birthday)) return "";
        var input = birthday.Trim();

        // Try month-first formats first (e.g., MM-dd, MM/dd)
        var monthFirstFormats = new[] { "MM-dd", "M-d", "MM/dd", "M/d" };
        if (DateTime.TryParseExact(input, monthFirstFormats, CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces, out var dtExact))
        {
            return dtExact.ToString("MMM d", CultureInfo.InvariantCulture) + " 🎂";
        }

        // Then try day-first interpretation of the same numeric patterns (e.g., dd-MM, dd/MM)
        var dayFirstFormats = new[] { "dd-MM", "d-M", "dd/MM", "d/M" };
        if (DateTime.TryParseExact(input, dayFirstFormats, CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces, out dtExact))
        {
            return dtExact.ToString("MMM d", CultureInfo.InvariantCulture) + " 🎂";
        }

        // Prefer explicit text patterns first to avoid misinterpreting day as year
        var enUS = CultureInfo.GetCultureInfo("en-US");
        var textFormats = new[]
        {
            "d MMM", "d MMMM", "MMM d", "MMMM d",
            "d MMM yyyy", "d MMMM yyyy", "MMM d yyyy", "MMMM d yyyy"
        };
        if (DateTime.TryParseExact(input, textFormats, enUS,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out var dt))
        {
            return dt.ToString("MMM d", CultureInfo.InvariantCulture) + " 🎂";
        }

        // Try with en-US explicitly (month names in English)
        if (DateTime.TryParse(input, enUS, DateTimeStyles.AllowWhiteSpaces, out dt))
        {
            return dt.ToString("MMM d", CultureInfo.InvariantCulture) + " 🎂";
        }

        return "";
    }
}
