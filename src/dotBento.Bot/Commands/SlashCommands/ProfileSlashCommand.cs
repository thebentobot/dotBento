using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using dotBento.Bot.AutoCompleteHandlers;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using dotBento.Bot.Services;
using dotBento.Infrastructure.Services;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

[SlashCommand("profile", "View and edit your Bento profile settings")]
public sealed class ProfileSlashCommand(
    InteractiveService interactiveService,
    ProfileEditCommand profileEditCommand,
    UserCommand userCommand,
    UserSettingService userSettingService,
    GuildMemberLookupService memberLookup)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    [SubSlashCommand("user", "Show a user's Bento profile")]
    public async Task ShowProfile([SlashCommandParameter(Name = "user", Description = "Pick a User")] User? user = null,
        [SlashCommandParameter(Name = "hide", Description = "Only show user info for you")]
        bool? hide = null)
    {
        await Context.DeferResponseAsync();
        user ??= Context.User;
        await user.ReturnIfBot(Context, interactiveService);
        var guildMember = Context.Guild is not null
            ? await memberLookup.GetOrFetchAsync(Context.Guild.Id, user.Id, Context.Guild)
            : null;
        var botPfp = Context.Client.Cache.User?.GetAvatarUrl()?.ToString(1024);
        await Context.SendFollowUpResponse(interactiveService,
            await userCommand.GetProfileAsync((long)user.Id,
                (long)Context.Guild!.Id,
                guildMember,
                Context.Guild?.UserCount ?? 0,
                botPfp),
            hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
    }

    [SubSlashCommand("background", "Background settings for your profile")]
    public sealed class BackgroundGroup(InteractiveService interactiveService, ProfileEditCommand profileEditCommand)
        : ApplicationCommandModule<ApplicationCommandContext>
    {
        [SubSlashCommand("url", "Set the background image URL for your profile")]
        public async Task SetBackgroundUrl([SlashCommandParameter(Name = "url", Description = "Direct image URL (https)")] string url)
        {
            var response = await profileEditCommand.SetBackgroundUrlAsync(Context.User.Id, url);
            await Context.SendResponse(interactiveService, response, true);
        }

        [SubSlashCommand("colour", "Set your profile background colour (hex)")]
        public async Task SetBackgroundColour(
            [SlashCommandParameter(Name = "hex", Description = "Hex colour like #1F2937 or 1F2937")]
            string hex,
            [SlashCommandParameter(Name = "opacity", Description = "Optional opacity 0-100")]
            int? opacity = null)
        {
            var response = await profileEditCommand.SetBackgroundColourAsync(Context.User.Id, hex, opacity);
            await Context.SendResponse(interactiveService, response, true);
        }

        [SubSlashCommand("upload", "Upload an image to use as your background")]
        public async Task UploadBackground(
            [SlashCommandParameter(Name = "image", Description = "Upload an image file")]
            Attachment image)
        {
            var url = image.Url;
            var response = await profileEditCommand.SetBackgroundUrlAsync(Context.User.Id, url);
            await Context.SendResponse(interactiveService, response, true);
        }

        [SubSlashCommand("reset", "Reset your background image and colour settings to default")]
        public async Task ResetBackground()
        {
            var response = await profileEditCommand.ResetBackgroundAsync(Context.User.Id);
            await Context.SendResponse(interactiveService, response, true);
        }
    }

    [SubSlashCommand("board", "Enable or disable boards on your profile")]
    public sealed class BoardsGroup(InteractiveService interactiveService, ProfileEditCommand profileEditCommand)
        : ApplicationCommandModule<ApplicationCommandContext>
    {
        [SubSlashCommand("lastfm", "Enable or disable your Last.fm board on your profile")]
        public async Task SetLastFmBoard([SlashCommandParameter(Name = "enabled", Description = "Enable (true) or disable (false)")] bool enabled)
        {
            var response = await profileEditCommand.SetLastFmBoardAsync(Context.User.Id, enabled);
            await Context.SendResponse(interactiveService, response, true);
        }

        [SubSlashCommand("xp", "Enable or disable your XP board on your profile")]
        public async Task SetXpBoard([SlashCommandParameter(Name = "enabled", Description = "Enable (true) or disable (false)")] bool enabled)
        {
            var response = await profileEditCommand.SetXpBoardAsync(Context.User.Id, enabled);
            await Context.SendResponse(interactiveService, response, true);
        }
    }

    [SubSlashCommand("description", "Set your profile description")]
    public async Task SetDescription(
        [SlashCommandParameter(Name = "text", Description = "Your description (max 500 characters)")]
        string text)
    {
        var response = await profileEditCommand.SetDescriptionAsync(Context.User.Id, text);
        await Context.SendResponse(interactiveService, response, true);
    }

    [SubSlashCommand("timezone", "Set your profile timezone (IANA/Windows ID)")]
    public async Task SetTimezone(
        [SlashCommandParameter(Name = "id", Description = "Timezone ID, e.g. Europe/Oslo or Pacific Standard Time", AutocompleteProviderType = typeof(TimezoneAutoComplete))]
        string id)
    {
        var response = await profileEditCommand.SetTimezoneAsync(Context.User.Id, id);
        await Context.SendResponse(interactiveService, response, true);
    }

    [SubSlashCommand("birthday", "Set your birthday (month and day only)")]
    public async Task SetBirthday(
        [SlashCommandParameter(Name = "date", Description = "Your birthday (MM-DD or M/D)")]
        string date)
    {
        var response = await profileEditCommand.SetBirthdayAsync(Context.User.Id, date);
        await Context.SendResponse(interactiveService, response, true);
    }

    [SubSlashCommand("reset", "Reset profile settings to default")]
    public sealed class ResetGroup(InteractiveService interactiveService, ProfileEditCommand profileEditCommand)
        : ApplicationCommandModule<ApplicationCommandContext>
    {
        [SubSlashCommand("background", "Reset your background image and colour settings to default")]
        public async Task ResetBackground()
        {
            var response = await profileEditCommand.ResetBackgroundAsync(Context.User.Id);
            await Context.SendResponse(interactiveService, response, true);
        }

        [SubSlashCommand("description", "Clear your profile description")]
        public async Task ResetDescription()
        {
            var response = await profileEditCommand.ResetDescriptionAsync(Context.User.Id);
            await Context.SendResponse(interactiveService, response, true);
        }

        [SubSlashCommand("timezone", "Clear your timezone")]
        public async Task ResetTimezone()
        {
            var response = await profileEditCommand.ResetTimezoneAsync(Context.User.Id);
            await Context.SendResponse(interactiveService, response, true);
        }

        [SubSlashCommand("birthday", "Clear your birthday")]
        public async Task ResetBirthday()
        {
            var response = await profileEditCommand.ResetBirthdayAsync(Context.User.Id);
            await Context.SendResponse(interactiveService, response, true);
        }
    }
}
