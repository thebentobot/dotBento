using System.Globalization;
using Discord;
using Discord.Interactions;
using dotBento.Bot.Commands.CommandHandlers;
using dotBento.Bot.Enums;
using dotBento.Bot.Models.Discord;
using dotBento.Infrastructure.Services;

namespace dotBento.Bot.ComponentInteractions;

public sealed class HoroscopeSettingsComponentInteraction(
    HoroscopeCommand horoscopeCommand, UserService userService)
    : InteractionModuleBase<IInteractionContext>
{
    [ComponentInteraction("horoscope-settings:sign:*")]
    public async Task SetSign(string ownerId, string[] selectedSigns)
    {
        if (!await EnsureOwnerAsync(ownerId))
            return;

        if (selectedSigns.Length != 1 ||
            !Enum.TryParse<ZodiacSign>(selectedSigns[0], true, out var sign) || !Enum.IsDefined(sign))
        {
            await RespondAsync("Choose a valid zodiac sign from `/horoscope settings`.", ephemeral: true);
            return;
        }

        await DeferAsync();
        await userService.CreateOrAddUserToCache(Context.User);
        var response = await horoscopeCommand.SaveHoroscopeAsync(
            (long)Context.User.Id, sign.ToString().ToLowerInvariant());
        await UpdatePanelAsync(response);
    }

    [ComponentInteraction("horoscope-settings:remove:*")]
    public async Task RemoveSign(string ownerId)
    {
        if (!await EnsureOwnerAsync(ownerId))
            return;

        await DeferAsync();
        var response = await horoscopeCommand.RemoveHoroscopeAsync((long)Context.User.Id);
        await UpdatePanelAsync(response);
    }

    private async Task<bool> EnsureOwnerAsync(string ownerId)
    {
        if (ownerId == Context.User.Id.ToString(CultureInfo.InvariantCulture))
            return true;

        await RespondAsync("Open `/horoscope settings` to manage your own saved sign.", ephemeral: true);
        return false;
    }

    private async Task UpdatePanelAsync(ResponseModel response) =>
        await ModifyOriginalResponseAsync(message =>
        {
            message.Embed = response.Embed.Build();
            message.Components = response.Components!.Build();
        });
}
