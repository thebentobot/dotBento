using NetCord;
using NetCord.Services.ApplicationCommands;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using dotBento.Infrastructure.Services;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

[SlashCommand("weather", "Check the weather!")]
public sealed class WeatherSlashCommand(InteractiveService interactiveService, WeatherCommand weatherCommand, UserSettingService userSettingService)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    [SubSlashCommand("check", "Check the weather for a user")]
    public async Task UserCommand([SlashCommandParameter(Name = "city", Description = "Check the weather in a city. Add country if it returns falsely")] string? city = null,
        [SlashCommandParameter(Name = "hide", Description = "Only show weather info for you")] bool? hide = null)
    {
        var guildUser = Context.Guild?.Users.GetValueOrDefault(Context.User.Id);
        var username = guildUser?.Nickname ?? Context.User.GlobalName;
        var userAvatar = guildUser?.GetGuildAvatarUrl()?.ToString(1024) ?? Context.User.GetAvatarUrl()?.ToString(1024);
        await Context.SendResponse(interactiveService, await weatherCommand.GetWeatherAsync((long)Context.User.Id, username, userAvatar, city), hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
    }

    [SubSlashCommand("set", "Set the city to check the weather for")]
    public async Task SetCommand([SlashCommandParameter(Name = "city", Description = "Set the city to check the weather for")] string city) =>
        await Context.SendResponse(interactiveService,
            await weatherCommand.SaveWeatherAsync((long)Context.User.Id, city));

    [SubSlashCommand("delete", "Delete the city to check the weather for")]
    public async Task DeleteCommand() => await Context.SendResponse(interactiveService,
        await weatherCommand.DeleteWeatherAsync((long)Context.User.Id));
}
