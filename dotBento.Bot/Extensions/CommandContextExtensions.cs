using Discord.Commands;
using dotBento.Domain;
using dotBento.Domain.Enums;
using Serilog;

namespace dotBento.Bot.Extensions;

public static class CommandContextExtensions
{
    public static void LogCommandUsed(this ICommandContext context, CommandResponse commandResponse = CommandResponse.Ok)
    {
        Log.Information("CommandUsed: {discordUserName} / {discordUserId} | {guildName} / {guildId} | {commandResponse} | {messageContent}",
            context.User?.Username, context.User?.Id, context.Guild?.Name, context.Guild?.Id, commandResponse, context.Message.Content);

        PublicProperties.UsedCommandsResponses.TryAdd(context.Message.Id, commandResponse);
    }
}