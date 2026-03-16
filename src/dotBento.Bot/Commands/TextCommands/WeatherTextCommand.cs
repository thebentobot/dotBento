using NetCord.Services.Commands;
using dotBento.Bot.Attributes;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using Fergun.Interactive;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Commands.TextCommands;

public sealed class WeatherTextCommand(
    IOptions<BotEnvConfig> botSettings,
    InteractiveService interactiveService, WeatherCommand weatherCommand) : BaseCommandModule(botSettings)
{

    [Command("weather")]
    [Summary("Check the weather for a city, either a saved city or a city you provide")]
    [Examples("weather", "weather Copenhagen")]
    public async Task WeatherCommand([CommandParameter(Remainder = true)] string? city = null)
    {
        _ = Context.Channel?.TriggerTypingAsync();
        var guildMember = Context.Guild?.Users.GetValueOrDefault(Context.User.Id);
        var username = guildMember?.Nickname ?? Context.User.GlobalName;
        var userAvatar = guildMember?.GetGuildAvatarUrl()?.ToString(1024) ?? Context.User.GetAvatarUrl()?.ToString(1024) ?? Context.User.DefaultAvatarUrl.ToString(1024);
        await Context.SendResponse(interactiveService, await weatherCommand.GetWeatherAsync((long)Context.User.Id, username, userAvatar, city));
    }
}
