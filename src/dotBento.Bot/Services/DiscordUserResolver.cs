using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace dotBento.Bot.Services;

public interface IDiscordUserResolver
{
    ValueTask<IUser?> GetUserAsync(ulong userId, RequestOptions? options = null);
}

public sealed class DiscordUserResolver(DiscordSocketClient client) : IDiscordUserResolver
{
    public ValueTask<IUser?> GetUserAsync(ulong userId, RequestOptions? options = null)
        => client.GetUserAsync(userId, options);
}
