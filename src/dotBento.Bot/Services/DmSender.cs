using System.Net;
using Discord;
using Discord.Net;
using dotBento.Bot.Resources;

namespace dotBento.Bot.Services;

public enum DmSendResult
{
    Success,
    Forbidden
}

public interface IDmSender
{
    Task<DmSendResult> SendReminderAsync(IUser user, string content);
}

public sealed class DmSender : IDmSender
{
    public async Task<DmSendResult> SendReminderAsync(IUser user, string content)
    {
        try
        {
            var dmChannel = await user.CreateDMChannelAsync();
            await dmChannel.SendMessageAsync(embed: new EmbedBuilder()
                .WithColor(DiscordConstants.BentoYellow)
                .WithTitle("Reminder")
                .WithDescription(content)
                .Build());
            return DmSendResult.Success;
        }
        catch (HttpException httpEx) when (
            httpEx.DiscordCode == DiscordErrorCode.CannotSendMessageToUser ||
            httpEx.HttpCode == HttpStatusCode.Forbidden)
        {
            // DM disabled, blocked, or lacking permission
            return DmSendResult.Forbidden;
        }
    }
}
