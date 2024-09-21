using System.Globalization;
using Discord;
using Discord.Commands;
using dotBento.Bot.Attributes;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using dotBento.Bot.Models.Discord;
using Fergun.Interactive;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Commands.TextCommands;

[Name("Remind")]
public class ReminderTextCommand(
    IOptions<BotEnvConfig> botSettings,
    InteractiveService interactiveService,
    ReminderCommand reminderCommand) : BaseCommandModule(botSettings)
{
    [Command("remind", RunMode = RunMode.Async)]
    [Summary(
        "Create, delete, or update reminders for yourself by content and date. Bento will remind you at the specified date and time. Date and time should be in the format `YYYY-MM-DDThh:mm[{+|-}hh:mm]` (e.g. 2022-12-31T23:59+00:00).")]
    [Alias("reminder", "notify")]
    [Examples(
        "remind create <YYYY-MM-DDThh:mm{+|-}hh:mm> <content>",
        "remind delete <reminderId>",
        "remind update <reminderId> <keep || YYYY-MM-DDThh:mm[{+|-}hh:mm]> [new content]",
        "remind list",
        "remind info <reminderId>"
    )]
    public async Task ReminderCommand([Remainder] string? input = null)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            await Context.SendResponse(interactiveService,
                ErrorEmbed(
                    "Please provide an argument to the command. You can check the usage of the command with the `remind help` command."));
            return;
        }

        _ = Context.Channel.TriggerTypingAsync();

        var args = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var command = args[0].ToLower();
        var userId = (long)Context.User.Id;

        switch (command)
        {
            case "create":
                if (args.Length < 3)
                {
                    await Context.SendResponse(interactiveService,
                        ErrorEmbed("Please provide a date and content for the reminder."));
                    return;
                }
                
                var date = args[1].ParseDateTimeOffset();
                if (date.HasNoValue)
                {
                    await Context.SendResponse(interactiveService,
                        ErrorEmbed("Invalid date. Please provide a valid date in the format `YYYY-MM-DDThh:mm[{+|-}hh:mm]` (e.g. 2022-12-31T23:59+00:00)."));
                    return;
                }

                var content = string.Join(' ', args[2..]);
                await Context.SendResponse(interactiveService,
                    await reminderCommand.CreateReminderAsync(userId, content, date.Value));
                break;
            case "delete":
                if (args.Length < 2)
                {
                    await Context.SendResponse(interactiveService,
                        ErrorEmbed("Please provide a reminder ID."));
                    return;
                }

                if (!int.TryParse(args[1], out var id))
                {
                    await Context.SendResponse(interactiveService,
                        ErrorEmbed("Invalid reminder ID. Please provide a valid reminder ID."));
                    return;
                }

                await Context.SendResponse(interactiveService,
                    await reminderCommand.DeleteReminderAsync(userId, id));
                break;
            case "update":
                if (args.Length < 4)
                {
                    await Context.SendResponse(interactiveService,
                        ErrorEmbed("Please provide a reminder ID, new date, and new content."));
                    return;
                }

                if (!int.TryParse(args[1], out var reminderId))
                {
                    await Context.SendResponse(interactiveService,
                        ErrorEmbed("Invalid reminder ID. Please provide a valid reminder ID."));
                    return;
                }
                
                if (args[2].Equals("keep", StringComparison.OrdinalIgnoreCase))
                {
                    var newContent = string.Join(' ', args[3..]);
                    await Context.SendResponse(interactiveService,
                        await reminderCommand.UpdateReminderAsync(userId, reminderId, newContent, null));
                }
                else
                {
                    var newDate = args[2].ParseDateTimeOffset();
                    if (newDate.HasNoValue)
                    {
                        await Context.SendResponse(interactiveService,
                            ErrorEmbed("Invalid date. Please provide a valid date in the format `YYYY-MM-DDThh:mm[{+|-}hh:mm]` (e.g. 2022-12-31T23:59+00:00)."));
                        return;
                    }

                    var newContent = string.Join(' ', args[3..]);
                    await Context.SendResponse(interactiveService,
                        await reminderCommand.UpdateReminderAsync(userId, reminderId, newContent, newDate.Value));
                }
                break;
            case "list":
                await Context.SendResponse(interactiveService,
                    await reminderCommand.GetRemindersAsync(userId));
                break;
            case "info":
                if (args.Length < 2)
                {
                    await Context.SendResponse(interactiveService,
                        ErrorEmbed("Please provide a reminder ID."));
                    return;
                }

                if (!int.TryParse(args[1], out var infoReminderId))
                {
                    await Context.SendResponse(interactiveService,
                        ErrorEmbed("Invalid reminder ID. Please provide a valid reminder ID."));
                    return;
                }

                await Context.SendResponse(interactiveService,
                    await reminderCommand.GetReminderAsync(userId, infoReminderId));
                break;
            default:
                await Context.SendResponse(interactiveService,
                    ErrorEmbed(
                        "Invalid command. You can check the usage of the command with the `remind help` command."));
                break;
        }
    }
    
    private static ResponseModel ErrorEmbed(string error)
    {
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        embed.Embed.WithTitle(error)
            .WithColor(Color.Red);
        return embed;
    }
}