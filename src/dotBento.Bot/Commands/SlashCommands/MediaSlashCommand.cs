using NetCord;
using NetCord.Services.ApplicationCommands;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Services;
using dotBento.Infrastructure.Services;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

[SlashCommand("media", "Retrieve media from social platforms")]
public sealed class MediaSlashCommand(
    InteractiveService interactiveService,
    MediaCommand mediaCommand,
    MediaRateLimitService rateLimitService,
    UserSettingService userSettingService)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    [SubSlashCommand("tiktok", "Retrieve media from a TikTok video")]
    public async Task TikTokCommand(
        [SlashCommandParameter(Name = "url", Description = "TikTok video URL")] string url,
        [SlashCommandParameter(Name = "hide", Description = "Only show the result for you")] bool? hide = null)
        => await HandleMediaCommand(url, hide, "tiktok");

    [SubSlashCommand("twitter", "Retrieve media from a Twitter/X post")]
    public async Task TwitterCommand(
        [SlashCommandParameter(Name = "url", Description = "Twitter or X post URL")] string url,
        [SlashCommandParameter(Name = "hide", Description = "Only show the result for you")] bool? hide = null)
        => await HandleMediaCommand(url, hide, "twitter");

    private async Task HandleMediaCommand(string url, bool? hide, string platform)
    {
        var memberCount     = Context.Guild?.UserCount ?? 0;
        var rateLimitResult = await rateLimitService.CheckAndRecordAsync(Context.User.Id, Context.Guild?.Id, platform, memberCount);
        if (!rateLimitResult.IsAllowed)
        {
            var msg = rateLimitResult.LimitType == "user"
                ? $"You're sending media commands too quickly. Try again in **{(int)rateLimitResult.RetryAfter!.Value.TotalSeconds}s**."
                : "This server is sending media commands too quickly. Try again in a moment.";
            await Context.SendResponse(interactiveService, new ResponseModel(ResponseType.Text, msg), ephemeral: true);
            return;
        }

        var effectiveHide = hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id);
        await Context.DeferResponseAsync(ephemeral: effectiveHide);

        try
        {
            var response = await mediaCommand.GetMediaResponseAsync(url);
            await Context.SendFollowUpResponse(interactiveService, response, effectiveHide);
        }
        catch (Exception ex)
        {
            await Context.HandleCommandException(ex);
        }
    }
}
