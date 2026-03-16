using NetCord;
using NetCord.Services.Commands;
using dotBento.Bot.Attributes;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using dotBento.Bot.Models.Discord;
using dotBento.Infrastructure.Utilities;
using Fergun.Interactive;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Commands.TextCommands;

[ModuleName("Tools")]
public sealed class ToolsTextCommand(
    IOptions<BotEnvConfig> botSettings,
    InteractiveService interactiveService,
    ToolsCommand toolsCommand) : BaseCommandModule(botSettings)
{
    [Command("colour", "color", "colors", "colours", "hex", "rgb")]
    [Summary("Get the colour of a hex code or RGB")]
    [Examples(
        "colour #FF0000",
        "color 255,0,0"
    )]
    public async Task GetColourCommand([CommandParameter(Remainder = true)] string colour) =>
        await Context.SendResponse(interactiveService, await toolsCommand.GetColour(colour));

    [Command("dominantColour", "dominantColor")]
    [Summary("Get the dominant colour of an image, either by URL or by attachment")]
    [Examples(
        "dominantColour",
        "dominantColor https://example.com/image.jpg"
    )]
    public async Task GetDominantColourCommand([CommandParameter(Remainder = true)] string? url = null)
    {
        var attachment = Context.Message.Attachments.FirstOrDefault();
        if (attachment != null)
        {
            url = attachment.Url;
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            await Context.SendResponse(interactiveService, ErrorEmbed("Please provide a URL or attach an image to the command."));
            return;
        }

        await Context.SendResponse(interactiveService, await toolsCommand.GetDominantColour(url));
    }

    [Command("timezone", "tz", "time")]
    [Summary("Show the current time in a timezone. Optionally provide a second timezone to compare against.")]
    [Examples(
        "timezone Europe/Copenhagen",
        "timezone Europe/Copenhagen America/New_York",
        "tz Asia/Tokyo Europe/London"
    )]
    public async Task GetTimezoneCommand([CommandParameter(Remainder = true)] string input)
    {
        var parts = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Try to find a split point where both halves are valid timezone IDs.
        // IANA IDs never contain spaces so a two-token split covers most cases;
        // multi-word Windows IDs are tried by walking the split point.
        string timezoneId = input.Trim();
        string? compareId = null;

        for (var i = parts.Length - 1; i >= 1; i--)
        {
            var candidate = string.Join(' ', parts[i..]);
            var main = string.Join(' ', parts[..i]);
            if (ProfileValidationUtilities.TryValidateTimezone(main) &&
                ProfileValidationUtilities.TryValidateTimezone(candidate))
            {
                timezoneId = main;
                compareId = candidate;
                break;
            }
        }

        await Context.SendResponse(interactiveService,
            await toolsCommand.GetTimezone(timezoneId, compareId, Context.User.Id));
    }

    private static ResponseModel ErrorEmbed(string error)
    {
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        embed.Embed.WithTitle(error)
            .WithColor(new Color(0xFF0000));
        return embed;
    }
}
