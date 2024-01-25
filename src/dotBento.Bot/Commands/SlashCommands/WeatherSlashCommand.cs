using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

[Group("weather", "Check the weather!")]
public class WeatherSlashCommand(InteractiveService interactiveService, WeatherCommand weatherCommand)
    : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("check", "Check the weather for a user")]
    public async Task UserCommand([Summary("city", "Check the weather in a city. Add country if it returns falsely")] string? city = null,
        [Summary("hide", "Only show weather info for you")] bool? hide = false)
    {
        var username = Context.Guild is null ? Context.User.GlobalName : Context.Guild.Users.First(x => x.Id == Context.User.Id).Nickname ?? Context.User.GlobalName;
        var userAvatar = Context.Guild is null ? Context.User.GetAvatarUrl() : Context.Guild.Users.First(x => x.Id == Context.User.Id).GetGuildAvatarUrl() ?? Context.User.GetDisplayAvatarUrl();
        await Context.SendResponse(interactiveService, await weatherCommand.GetWeatherAsync((long)Context.User.Id, username, userAvatar, city), hide ?? false);
    }
    
    [SlashCommand("set", "Set the city to check the weather for")]
    public async Task SetCommand([Summary("city", "Set the city to check the weather for")] string city) =>
        await Context.SendResponse(interactiveService,
            await weatherCommand.SaveWeatherAsync((long)Context.User.Id, city));
    
    [SlashCommand("delete", "Delete the city to check the weather for")]
    public async Task DeleteCommand() => await Context.SendResponse(interactiveService,
        await weatherCommand.DeleteWeatherAsync((long)Context.User.Id));
}