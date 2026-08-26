using Discord.Commands;
using dotBento.Bot.Attributes;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using dotBento.Bot.Models.Discord;
using Fergun.Interactive;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Commands.TextCommands;

public sealed class HoroscopeTextCommand(
    IOptions<BotEnvConfig> botSettings,
    InteractiveService interactiveService,
    HoroscopeCommand horoscopeCommand) : BaseCommandModule(botSettings)
{
    [Command("horoscope", RunMode = RunMode.Async)]
    [Alias("horo", "astro", "zodiac", "hs")]
    [Summary("Read a horoscope or save your zodiac sign")]
    [Examples("horoscope", "horoscope tomorrow libra", "horoscope week", "hs save aries", "zodiac list")]
    public async Task HoroscopeCommand([Remainder] string? input = null)
    {
        _ = Context.Channel.TriggerTypingAsync();
        var args = input?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];

        ResponseModel response;
        if (args.Length == 0)
        {
            response = await horoscopeCommand.GetHoroscopeAsync((long)Context.User.Id, "today");
        }
        else
        {
            response = args[0].ToLowerInvariant() switch
            {
                "save" => await horoscopeCommand.SaveHoroscopeAsync(
                    (long)Context.User.Id,
                    args.Length > 1 ? args[1] : null),
                "remove" or "delete" => await horoscopeCommand.RemoveHoroscopeAsync((long)Context.User.Id),
                "list" => horoscopeCommand.ListHoroscopes(),
                _ => await horoscopeCommand.GetHoroscopeAsync(
                    (long)Context.User.Id,
                    args[0],
                    args.Length > 1 ? args[1] : null)
            };
        }

        await Context.SendResponse(interactiveService, response);
    }
}
