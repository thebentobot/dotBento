using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.AutoCompleteHandlers;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

[Group("profile", "View and edit your Bento profile settings")]
public sealed class ProfileSlashCommand(
    InteractiveService interactiveService,
    ProfileEditCommand profileEditCommand,
    UserCommand userCommand)
    : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("user", "Show a user's Bento profile")]
    public async Task ShowProfile([Summary("user", "Pick a User")] SocketUser? user = null,
        [Summary("hide", "Only show user info for you")]
        bool? hide = false)
    {
        _ = DeferAsync();
        user ??= Context.User;
        await user.ReturnIfBot(Context, interactiveService);
        var guildMember = Context.Guild.Users.First(x => x.Id == user.Id);
        var botPfp = Context.Client.CurrentUser.GetDisplayAvatarUrl();
        await Context.SendFollowUpResponse(interactiveService,
            await userCommand.GetProfileAsync((long)user.Id,
                (long)Context.Guild.Id,
                guildMember,
                Context.Guild.MemberCount,
                botPfp),
            hide ?? false);
    }

    [Group("background", "Background settings for your profile")]
    public sealed class BackgroundGroup(InteractiveService interactiveService, ProfileEditCommand profileEditCommand)
        : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("url", "Set the background image URL for your profile")]
        public async Task SetBackgroundUrl([Summary("url", "Direct image URL (http/https)")] string url)
        {
            var response = await profileEditCommand.SetBackgroundUrlAsync(Context.User.Id, url);
            await Context.SendResponse(interactiveService, response, true);
        }

        [SlashCommand("colour", "Set your profile background colour (hex)")]
        public async Task SetBackgroundColour(
            [Summary("hex", "Hex colour like #1F2937 or 1F2937")]
            string hex,
            [Summary("opacity", "Optional opacity 0-100")]
            int? opacity = null)
        {
            var response = await profileEditCommand.SetBackgroundColourAsync(Context.User.Id, hex, opacity);
            await Context.SendResponse(interactiveService, response, true);
        }

        [SlashCommand("upload", "Upload an image to use as your background")]
        public async Task UploadBackground(
            [Summary("image", "Upload an image file")]
            IAttachment image)
        {
            var url = image.Url;
            var response = await profileEditCommand.SetBackgroundUrlAsync(Context.User.Id, url);
            await Context.SendResponse(interactiveService, response, true);
        }

        [SlashCommand("reset", "Reset your background image and colour settings to default")]
        public async Task ResetBackground()
        {
            var response = await profileEditCommand.ResetBackgroundAsync(Context.User.Id);
            await Context.SendResponse(interactiveService, response, true);
        }
    }

    [Group("board", "Enable or disable boards on your profile")]
    public sealed class BoardsGroup(InteractiveService interactiveService, ProfileEditCommand profileEditCommand)
        : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("lastfm", "Enable or disable your Last.fm board on your profile")]
        public async Task SetLastFmBoard([Summary("enabled", "Enable (true) or disable (false)")] bool enabled)
        {
            var response = await profileEditCommand.SetLastFmBoardAsync(Context.User.Id, enabled);
            await Context.SendResponse(interactiveService, response, true);
        }

        [SlashCommand("xp", "Enable or disable your XP board on your profile")]
        public async Task SetXpBoard([Summary("enabled", "Enable (true) or disable (false)")] bool enabled)
        {
            var response = await profileEditCommand.SetXpBoardAsync(Context.User.Id, enabled);
            await Context.SendResponse(interactiveService, response, true);
        }
    }

    [SlashCommand("description", "Set your profile description")]
    public async Task SetDescription(
        [Summary("text", "Your description (max 500 characters)")]
        string text)
    {
        var response = await profileEditCommand.SetDescriptionAsync(Context.User.Id, text);
        await Context.SendResponse(interactiveService, response, true);
    }

    [SlashCommand("timezone", "Set your profile timezone (IANA/Windows ID)")]
    public async Task SetTimezone(
        [Summary("id", "Timezone ID, e.g. Europe/Oslo or Pacific Standard Time")]
        [Autocomplete(typeof(TimezoneAutoComplete))]
        string id)
    {
        var response = await profileEditCommand.SetTimezoneAsync(Context.User.Id, id);
        await Context.SendResponse(interactiveService, response, true);
    }

    [SlashCommand("birthday", "Set your birthday (month and day only)")]
    public async Task SetBirthday(
        [Summary("date", "Your birthday (MM-DD or M/D)")]
        string date)
    {
        var response = await profileEditCommand.SetBirthdayAsync(Context.User.Id, date);
        await Context.SendResponse(interactiveService, response, true);
    }

    [Group("reset", "Reset profile settings to default")]
    public sealed class ResetGroup(InteractiveService interactiveService, ProfileEditCommand profileEditCommand)
        : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("background", "Reset your background image and colour settings to default")]
        public async Task ResetBackground()
        {
            var response = await profileEditCommand.ResetBackgroundAsync(Context.User.Id);
            await Context.SendResponse(interactiveService, response, true);
        }

        [SlashCommand("description", "Clear your profile description")]
        public async Task ResetDescription()
        {
            var response = await profileEditCommand.ResetDescriptionAsync(Context.User.Id);
            await Context.SendResponse(interactiveService, response, true);
        }

        [SlashCommand("timezone", "Clear your timezone")]
        public async Task ResetTimezone()
        {
            var response = await profileEditCommand.ResetTimezoneAsync(Context.User.Id);
            await Context.SendResponse(interactiveService, response, true);
        }

        [SlashCommand("birthday", "Clear your birthday")]
        public async Task ResetBirthday()
        {
            var response = await profileEditCommand.ResetBirthdayAsync(Context.User.Id);
            await Context.SendResponse(interactiveService, response, true);
        }
    }
}