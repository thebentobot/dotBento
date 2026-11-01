using Discord.WebSocket;
using dotBento.Domain;

namespace dotBento.Bot.Handlers;

/// <summary>
/// Keeps the gateway gauges current so a dashboard can tell a dead connection from a quiet bot.
/// </summary>
public sealed class GatewayMetricsHandler : IDisposable
{
    private readonly DiscordSocketClient _client;

    public GatewayMetricsHandler(DiscordSocketClient client)
    {
        _client = client;
        _client.Connected += OnConnected;
        _client.Disconnected += OnDisconnected;
        _client.Ready += OnReady;
        _client.LatencyUpdated += OnLatencyUpdated;
    }

    private Task OnConnected()
    {
        Statistics.DiscordGatewayConnected.Set(1);
        return Task.CompletedTask;
    }

    private Task OnReady()
    {
        Statistics.DiscordGatewayConnected.Set(1);
        Statistics.DiscordGatewayLatency.Set(_client.Latency);
        return Task.CompletedTask;
    }

    private Task OnDisconnected(Exception exception)
    {
        Statistics.DiscordGatewayConnected.Set(0);
        Statistics.DiscordGatewayReconnects.Inc();
        return Task.CompletedTask;
    }

    private Task OnLatencyUpdated(int oldLatency, int newLatency)
    {
        Statistics.DiscordGatewayLatency.Set(newLatency);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _client.Connected -= OnConnected;
        _client.Disconnected -= OnDisconnected;
        _client.Ready -= OnReady;
        _client.LatencyUpdated -= OnLatencyUpdated;
    }
}
