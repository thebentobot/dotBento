using Discord.Commands;
using dotBento.Bot.Attributes;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using Fergun.Interactive;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Commands.TextCommands;

public class DeleteWeatherTextCommand(
    IOptions<BotEnvConfig> botSettings,
    InteractiveService interactiveService, WeatherCommand weatherCommand) : BaseCommandModule(botSettings)
{

    [Command("deleteWeather", RunMode = RunMode.Async)]
    [Summary("Delete a saved city for the weather command")]
    [Examples("deleteWeather")]
    public async Task RemoveWeatherCommand()
    {
        _ = Context.Channel.TriggerTypingAsync();
        await Context.SendResponse(interactiveService, await weatherCommand.DeleteWeatherAsync((long)Context.User.Id));
    }
}