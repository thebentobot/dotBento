using Discord.Commands;
using dotBento.Bot.Attributes;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using Fergun.Interactive;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Commands.TextCommands
{
    public class SaveWeatherTextCommand(
        IOptions<BotEnvConfig> botSettings,
        InteractiveService interactiveService, WeatherCommand weatherCommand) : BaseCommandModule(botSettings)
    {

        [Command("saveWeather", RunMode = RunMode.Async)]
        [Summary("Save a city to check the weather for without having to provide it every time")]
        [Examples("saveWeather Guangzhou")]
        public async Task SaveWeatherCommand([Remainder] string city)
        {
            _ = Context.Channel.TriggerTypingAsync();
            await Context.SendResponse(interactiveService, await weatherCommand.SaveWeatherAsync((long)Context.User.Id, city));
        }
    }
}