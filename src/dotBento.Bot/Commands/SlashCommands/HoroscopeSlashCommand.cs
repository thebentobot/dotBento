using dotBento.Bot.Services;
using Discord.Interactions;
using dotBento.Bot.Commands.CommandHandlers;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Infrastructure.Services;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

[Group("horoscope", "Read and save zodiac horoscopes")]
public sealed class HoroscopeSlashCommand(
    InteractiveService interactiveService,
    HoroscopeCommand horoscopeCommand,
    UserSettingService userSettingService, BotPresentation? presentation = null) : ConfiguredInteractionModule<SocketInteractionContext>(presentation)
{
    [SlashCommand("today", "Read today's horoscope")]
    public Task TodayCommand(
        [Summary("sign", "Use a zodiac sign without changing your saved sign")] ZodiacSign? sign = null,
        [Summary("hide", "Only show the horoscope to you")] bool? hide = null) => SendReading("today", sign, hide);

    [SlashCommand("yesterday", "Read yesterday's horoscope")]
    public Task YesterdayCommand(
        [Summary("sign", "Use a zodiac sign without changing your saved sign")] ZodiacSign? sign = null,
        [Summary("hide", "Only show the horoscope to you")] bool? hide = null) => SendReading("yesterday", sign, hide);

    [SlashCommand("tomorrow", "Read tomorrow's horoscope")]
    public Task TomorrowCommand(
        [Summary("sign", "Use a zodiac sign without changing your saved sign")] ZodiacSign? sign = null,
        [Summary("hide", "Only show the horoscope to you")] bool? hide = null) => SendReading("tomorrow", sign, hide);

    [SlashCommand("weekly", "Read this week's horoscope")]
    public Task WeeklyCommand(
        [Summary("sign", "Use a zodiac sign without changing your saved sign")] ZodiacSign? sign = null,
        [Summary("hide", "Only show the horoscope to you")] bool? hide = null) => SendReading("weekly", sign, hide);

    [SlashCommand("monthly", "Read this month's horoscope")]
    public Task MonthlyCommand(
        [Summary("sign", "Use a zodiac sign without changing your saved sign")] ZodiacSign? sign = null,
        [Summary("hide", "Only show the horoscope to you")] bool? hide = null) => SendReading("monthly", sign, hide);

    [SlashCommand("settings", "Choose or remove your saved zodiac sign")]
    public async Task SettingsCommand()
    {
        await DeferAsync(ephemeral: true);
        await Context.SendFollowUpResponse(
            interactiveService,
            await horoscopeCommand.GetSettingsAsync((long)Context.User.Id),
            ephemeral: true);
    }

    private async Task SendReading(string window, ZodiacSign? sign, bool? hide)
    {
        var ephemeral = hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id);
        await DeferAsync(ephemeral: ephemeral);
        await Context.SendFollowUpResponse(
            interactiveService,
            await horoscopeCommand.GetHoroscopeAsync(
                (long)Context.User.Id, window, sign?.ToString().ToLowerInvariant(), Context.Channel),
            ephemeral);
    }
}
