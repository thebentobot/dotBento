using Discord;
using Discord.Interactions;
using dotBento.Bot.AutoCompleteHandlers;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models.Discord;
using dotBento.Infrastructure.Services;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

[Group("tools", "Small tools for convenience")]
public sealed class ToolsSlashCommand(InteractiveService interactiveService, ToolsCommand toolsCommand, UserSettingService userSettingService) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("colour", "Get the colour of a hex code or RGB")]
    public async Task GetColourCommand(
        [Summary("colour", "Hex code or RGB separated by commas")] string colour,
        [Summary("hide", "Only show colour for you")] bool? hide = null) =>
        await Context.SendResponse(interactiveService, await toolsCommand.GetColour(colour), hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));

    [SlashCommand("dominantcolour", "Get the dominant colour of an image, either by URL or by attachment")]
    public async Task GetDominantColourCommand(
        [Summary("url", "URL of the image")] string? url,
        [Summary("attachment", "Add a photo")] IAttachment? attachment = null,
        [Summary("hide", "Only show colour for you")] bool? hide = null)
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

    [SlashCommand("timezone", "Show the current time in a timezone")]
    public async Task GetTimezoneCommand(
        [Summary("timezone", "Timezone to look up, e.g. Europe/Copenhagen")]
        [Autocomplete(typeof(TimezoneAutoComplete))]
        string timezone,
        [Summary("compare", "Timezone to compare against (defaults to your profile timezone if set)")]
        [Autocomplete(typeof(TimezoneAutoComplete))]
        string? compare = null,
        [Summary("hide", "Only show this result for you")]
        bool? hide = null) =>
        await Context.SendResponse(
            interactiveService,
            await toolsCommand.GetTimezone(timezone, compare, Context.User.Id),
            hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));

    private static ResponseModel ErrorEmbed(string error)
    {
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        embed.Embed.WithTitle(error)
            .WithColor(Color.Red);
        return embed;
    }
}
