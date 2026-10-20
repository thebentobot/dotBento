using dotBento.Bot.Services;
using Discord.Interactions;
using dotBento.Bot.Commands.CommandHandlers;
using dotBento.Bot.Extensions;
using dotBento.Infrastructure.Services;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

[Group("weather", "Check the weather!")]
public sealed class WeatherSlashCommand(InteractiveService interactiveService, WeatherCommand weatherCommand, UserSettingService userSettingService, IDiscordGuildMemberResolver guildMembers, BotPresentation? presentation = null)
    : ConfiguredInteractionModule<SocketInteractionContext>(presentation)
{
    [SlashCommand("check", "Check the weather for a user")]
    public async Task UserCommand([Summary("city", "Check the weather in a city. Add country if it returns falsely")] string? city = null,
        [Summary("hide", "Only show weather info for you")] bool? hide = null)
    {
        await DeferAsync(ephemeral: hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
        var (username, userAvatar) = await Context.MemberAppearanceAsync(guildMembers, Context.User);
        await Context.SendFollowUpResponse(interactiveService, await weatherCommand.GetWeatherAsync((long)Context.User.Id, username, userAvatar, city), hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
    }

    [SlashCommand("set", "Set the city to check the weather for")]
    public async Task SetCommand([Summary("city", "Set the city to check the weather for")] string city) =>
        await Context.SendResponse(interactiveService,
            await weatherCommand.SaveWeatherAsync((long)Context.User.Id, city));

    [SlashCommand("delete", "Delete the city to check the weather for")]
    public async Task DeleteCommand() => await Context.SendResponse(interactiveService,
        await weatherCommand.DeleteWeatherAsync((long)Context.User.Id));
}
