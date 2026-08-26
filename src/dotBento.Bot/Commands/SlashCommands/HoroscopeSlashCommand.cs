using Discord.Interactions;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using dotBento.Infrastructure.Services;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

[Group("horoscope", "Read and save zodiac horoscopes")]
public sealed class HoroscopeSlashCommand(
    InteractiveService interactiveService,
    HoroscopeCommand horoscopeCommand,
    UserSettingService userSettingService) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("today", "Read today's horoscope")]
    public Task TodayCommand(
        [Summary("sign", "Use a zodiac sign without changing your saved sign")] string? sign = null,
        [Summary("hide", "Only show the horoscope to you")] bool? hide = null) => SendReading("today", sign, hide);

    [SlashCommand("yesterday", "Read yesterday's horoscope")]
    public Task YesterdayCommand(
        [Summary("sign", "Use a zodiac sign without changing your saved sign")] string? sign = null,
        [Summary("hide", "Only show the horoscope to you")] bool? hide = null) => SendReading("yesterday", sign, hide);

    [SlashCommand("tomorrow", "Read tomorrow's horoscope")]
    public Task TomorrowCommand(
        [Summary("sign", "Use a zodiac sign without changing your saved sign")] string? sign = null,
        [Summary("hide", "Only show the horoscope to you")] bool? hide = null) => SendReading("tomorrow", sign, hide);

    [SlashCommand("weekly", "Read this week's horoscope")]
    public Task WeeklyCommand(
        [Summary("sign", "Use a zodiac sign without changing your saved sign")] string? sign = null,
        [Summary("hide", "Only show the horoscope to you")] bool? hide = null) => SendReading("weekly", sign, hide);

    [SlashCommand("monthly", "Read this month's horoscope")]
    public Task MonthlyCommand(
        [Summary("sign", "Use a zodiac sign without changing your saved sign")] string? sign = null,
        [Summary("hide", "Only show the horoscope to you")] bool? hide = null) => SendReading("monthly", sign, hide);

    [SlashCommand("save", "Save your zodiac sign")]
    public async Task SaveCommand([Summary("sign", "The zodiac sign to save")] string sign) =>
        await Context.SendResponse(
            interactiveService,
            await horoscopeCommand.SaveHoroscopeAsync((long)Context.User.Id, sign),
            true);

    [SlashCommand("remove", "Remove your saved zodiac sign")]
    public async Task RemoveCommand() =>
        await Context.SendResponse(
            interactiveService,
            await horoscopeCommand.RemoveHoroscopeAsync((long)Context.User.Id),
            true);

    [SlashCommand("list", "List all zodiac signs")]
    public async Task ListCommand() =>
        await Context.SendResponse(interactiveService, horoscopeCommand.ListHoroscopes());

    private async Task SendReading(string window, string? sign, bool? hide)
    {
        var ephemeral = hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id);
        await DeferAsync(ephemeral: ephemeral);
        await Context.SendFollowUpResponse(
            interactiveService,
            await horoscopeCommand.GetHoroscopeAsync((long)Context.User.Id, window, sign),
            ephemeral);
    }
}
