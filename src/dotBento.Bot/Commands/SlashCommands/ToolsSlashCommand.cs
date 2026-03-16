using NetCord;
using NetCord.Services.ApplicationCommands;
using dotBento.Bot.AutoCompleteHandlers;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models.Discord;
using dotBento.Infrastructure.Services;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

[SlashCommand("tools", "Small tools for convenience")]
public sealed class ToolsSlashCommand(InteractiveService interactiveService, ToolsCommand toolsCommand, UserSettingService userSettingService) : ApplicationCommandModule<ApplicationCommandContext>
{
    [SubSlashCommand("colour", "Get the colour of a hex code or RGB")]
    public async Task GetColourCommand(
        [SlashCommandParameter(Name = "colour", Description = "Hex code or RGB separated by commas")] string colour,
        [SlashCommandParameter(Name = "hide", Description = "Only show colour for you")] bool? hide = null) =>
        await Context.SendResponse(interactiveService, await toolsCommand.GetColour(colour), hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));

    [SubSlashCommand("dominantcolour", "Get the dominant colour of an image, either by URL or by attachment")]
    public async Task GetDominantColourCommand(
        [SlashCommandParameter(Name = "url", Description = "URL of the image")] string? url = null,
        [SlashCommandParameter(Name = "attachment", Description = "Add a photo")] Attachment? attachment = null,
        [SlashCommandParameter(Name = "hide", Description = "Only show colour for you")] bool? hide = null)
    {
        if (attachment != null)
        {
            url = attachment.Url;
        }

        var effectiveHide = hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id);
        if (string.IsNullOrWhiteSpace(url))
        {
            await Context.SendResponse(interactiveService, ErrorEmbed("Please provide a URL or attach an image to the command."), effectiveHide);
            return;
        }

        await Context.SendResponse(interactiveService, await toolsCommand.GetDominantColour(url), effectiveHide);
    }

    [SubSlashCommand("timezone", "Show the current time in a timezone")]
    public async Task GetTimezoneCommand(
        [SlashCommandParameter(Name = "timezone", Description = "Timezone to look up, e.g. Europe/Copenhagen", AutocompleteProviderType = typeof(TimezoneAutoComplete))]
        string timezone,
        [SlashCommandParameter(Name = "compare", Description = "Timezone to compare against (defaults to your profile timezone if set)", AutocompleteProviderType = typeof(TimezoneAutoComplete))]
        string? compare = null,
        [SlashCommandParameter(Name = "hide", Description = "Only show this result for you")]
        bool? hide = null) =>
        await Context.SendResponse(
            interactiveService,
            await toolsCommand.GetTimezone(timezone, compare, Context.User.Id),
            hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));

    private static ResponseModel ErrorEmbed(string error)
    {
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        embed.Embed.WithTitle(error)
            .WithColor(new Color(0xFF0000));
        return embed;
    }
}
