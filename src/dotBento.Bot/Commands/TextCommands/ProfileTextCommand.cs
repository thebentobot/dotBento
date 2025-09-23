using Discord;
using Discord.Commands;
using Discord.WebSocket;
using dotBento.Bot.Attributes;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using Fergun.Interactive;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Commands.TextCommands;

// TODO - refine these a bit the day text commands are used in production
public sealed class ProfileTextCommand(
    IOptions<BotEnvConfig> botSettings,
    InteractiveService interactiveService,
    ProfileEditCommand profileEditCommand,
    UserCommand userCommand) : BaseCommandModule(botSettings)
{
    [GuildOnly]
    [Command("profile", RunMode = RunMode.Async)]
    [Summary("Show a user's Bento profile")]
    [Examples("profile", "profile @SomeUser")]
    public async Task ShowProfile(SocketUser? user = null)
    {
        _ = Context.Channel.TriggerTypingAsync();
        user ??= Context.User;
        await user.ReturnIfBot(Context, interactiveService);
        var guildMember = Context.Guild.Users.First(x => x.Id == user.Id);
        var botPfp = Context.Client.CurrentUser.GetDisplayAvatarUrl();
        await Context.SendResponse(interactiveService,
            await userCommand.GetProfileAsync((long)user.Id,
                (long)Context.Guild.Id,
                guildMember,
                Context.Guild.MemberCount,
                botPfp));
    }

    [Command("profileBackgroundUrl", RunMode = RunMode.Async)]
    [Summary("Set the background image URL for your profile")]
    [Examples("profileBackgroundUrl https://example.com/image.png")]
    public async Task SetBackgroundUrl([Remainder] string url)
    {
        _ = Context.Channel.TriggerTypingAsync();
        var response = await profileEditCommand.SetBackgroundUrlAsync(Context.User.Id, url);
        await Context.SendResponse(interactiveService, response);
    }

    [Command("profileBackgroundColour", RunMode = RunMode.Async)]
    [Summary("Set your profile background colour (hex) with optional opacity")]
    [Examples("profileBackgroundColour #1F2937 80", "profileBackgroundColour 1F2937")]
    public async Task SetBackgroundColour(string hex, int? opacity = null)
    {
        _ = Context.Channel.TriggerTypingAsync();
        var response = await profileEditCommand.SetBackgroundColourAsync(Context.User.Id, hex, opacity);
        await Context.SendResponse(interactiveService, response);
    }

    [Command("profileBackgroundUpload", RunMode = RunMode.Async)]
    [Summary("Use the first image attachment in your message as background")]
    [Examples("profileBackgroundUpload [attach an image]")]
    public async Task UploadBackground()
    {
        _ = Context.Channel.TriggerTypingAsync();
        var attachment = Context.Message.Attachments.FirstOrDefault();
        if (attachment == null)
        {
            await ReplyAsync("Please attach an image to your message.");
            return;
        }

        var response = await profileEditCommand.SetBackgroundUrlAsync(Context.User.Id, attachment.Url);
        await Context.SendResponse(interactiveService, response);
    }

    [Command("profileResetBackground", RunMode = RunMode.Async)]
    [Summary("Reset your background image and colour settings to default")]
    [Examples("profileResetBackground")]
    public async Task ResetBackground()
    {
        _ = Context.Channel.TriggerTypingAsync();
        var response = await profileEditCommand.ResetBackgroundAsync(Context.User.Id);
        await Context.SendResponse(interactiveService, response);
    }

    [Command("profileBoardsLastfm", RunMode = RunMode.Async)]
    [Summary("Enable or disable your Last.fm board on your profile")]
    [Examples("profileBoardsLastfm true", "profileBoardsLastfm false")]
    public async Task SetLastFmBoard(bool enabled)
    {
        _ = Context.Channel.TriggerTypingAsync();
        var response = await profileEditCommand.SetLastFmBoardAsync(Context.User.Id, enabled);
        await Context.SendResponse(interactiveService, response);
    }

    [Command("profileBoardsXp", RunMode = RunMode.Async)]
    [Summary("Enable or disable your XP board on your profile")]
    [Examples("profileBoardsXp true", "profileBoardsXp false")]
    public async Task SetXpBoard(bool enabled)
    {
        _ = Context.Channel.TriggerTypingAsync();
        var response = await profileEditCommand.SetXpBoardAsync(Context.User.Id, enabled);
        await Context.SendResponse(interactiveService, response);
    }

    [Command("profileDescription", RunMode = RunMode.Async)]
    [Summary("Set your profile description")]
    [Examples("profileDescription I like sushi and bento boxes!")]
    public async Task SetDescription([Remainder] string text)
    {
        _ = Context.Channel.TriggerTypingAsync();
        var response = await profileEditCommand.SetDescriptionAsync(Context.User.Id, text);
        await Context.SendResponse(interactiveService, response);
    }

    [Command("profileTimezone", RunMode = RunMode.Async)]
    [Summary("Set your profile timezone (IANA/Windows ID)")]
    [Examples("profileTimezone Europe/Oslo")]
    public async Task SetTimezone([Remainder] string id)
    {
        _ = Context.Channel.TriggerTypingAsync();
        var response = await profileEditCommand.SetTimezoneAsync(Context.User.Id, id);
        await Context.SendResponse(interactiveService, response);
    }

    [Command("profileBirthday", RunMode = RunMode.Async)]
    [Summary("Set your birthday (month and day only)")]
    [Examples("profileBirthday 07-21", "profileBirthday 7/21")]
    public async Task SetBirthday([Remainder] string date)
    {
        _ = Context.Channel.TriggerTypingAsync();
        var response = await profileEditCommand.SetBirthdayAsync(Context.User.Id, date);
        await Context.SendResponse(interactiveService, response);
    }

    [Command("profileResetDescription", RunMode = RunMode.Async)]
    [Summary("Clear your profile description")]
    [Examples("profileResetDescription")]
    public async Task ResetDescription()
    {
        _ = Context.Channel.TriggerTypingAsync();
        var response = await profileEditCommand.ResetDescriptionAsync(Context.User.Id);
        await Context.SendResponse(interactiveService, response);
    }

    [Command("profileResetTimezone", RunMode = RunMode.Async)]
    [Summary("Clear your timezone")]
    [Examples("profileResetTimezone")]
    public async Task ResetTimezone()
    {
        _ = Context.Channel.TriggerTypingAsync();
        var response = await profileEditCommand.ResetTimezoneAsync(Context.User.Id);
        await Context.SendResponse(interactiveService, response);
    }

    [Command("profileResetBirthday", RunMode = RunMode.Async)]
    [Summary("Clear your birthday")]
    [Examples("profileResetBirthday")]
    public async Task ResetBirthday()
    {
        _ = Context.Channel.TriggerTypingAsync();
        var response = await profileEditCommand.ResetBirthdayAsync(Context.User.Id);
        await Context.SendResponse(interactiveService, response);
    }
}