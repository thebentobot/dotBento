using Discord.Commands;
using dotBento.Bot.Attributes;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using Fergun.Interactive;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Commands.TextCommands;

public class WeatherTextCommand(
    IOptions<BotEnvConfig> botSettings,
    InteractiveService interactiveService, WeatherCommand weatherCommand) : BaseCommandModule(botSettings)
{

    [Command("weather", RunMode = RunMode.Async)]
    [Summary("Check the weather for a city, either a saved city or a city you provide")]
    [Examples("weather", "weather Copenhagen")]
    public async Task WeatherCommand([Remainder] string? city = null)
    {
        _ = Context.Channel.TriggerTypingAsync();
        var username = Context.Guild is null ? Context.User.GlobalName : Context.Guild.Users.First(x => x.Id == Context.User.Id).Nickname ?? Context.User.GlobalName;
        var userAvatar = Context.Guild is null ? Context.User.GetAvatarUrl() : Context.Guild.Users.First(x => x.Id == Context.User.Id).GetGuildAvatarUrl() ?? Context.User.GetDisplayAvatarUrl();
        await Context.SendResponse(interactiveService, await weatherCommand.GetWeatherAsync((long)Context.User.Id, username, userAvatar, city));
    }
}