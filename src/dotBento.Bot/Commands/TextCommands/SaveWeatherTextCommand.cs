using NetCord.Services.Commands;
using dotBento.Bot.Attributes;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using Fergun.Interactive;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Commands.TextCommands;

public sealed class SaveWeatherTextCommand(
    IOptions<BotEnvConfig> botSettings,
    InteractiveService interactiveService, WeatherCommand weatherCommand) : BaseCommandModule(botSettings)
{

    [Command("saveWeather")]
    [Summary("Save a city to check the weather for without having to provide it every time")]
    [Examples("saveWeather Guangzhou")]
    public async Task SaveWeatherCommand([CommandParameter(Remainder = true)] string city)
    {
        _ = Context.Channel?.TriggerTypingAsync();
        await Context.SendResponse(interactiveService, await weatherCommand.SaveWeatherAsync((long)Context.User.Id, city));
    }
}
