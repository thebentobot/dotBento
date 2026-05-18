using System.Net;
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using dotBento.Bot.Resources;

namespace dotBento.Bot.Services;

public enum DmSendResult
{
    Success,
    Forbidden
}

public interface IDmSender
{
    Task<DmSendResult> SendReminderAsync(ulong userId, string content);
}

public sealed class DmSender(GatewayClient client) : IDmSender
{
    public async Task<DmSendResult> SendReminderAsync(ulong userId, string content)
    {
        try
        {
            var dmChannel = await client.Rest.GetDMChannelAsync(userId);
            await dmChannel.SendMessageAsync(new MessageProperties()
                .AddEmbeds(new EmbedProperties()
                    .WithColor(DiscordConstants.BentoYellow)
                    .WithTitle("Reminder")
                    .WithDescription(content)));
            return DmSendResult.Success;
        }
        catch (RestException restEx) when (restEx.StatusCode == HttpStatusCode.Forbidden)
        {
            // DM disabled, blocked, or lacking permission
            return DmSendResult.Forbidden;
        }
    }
}
