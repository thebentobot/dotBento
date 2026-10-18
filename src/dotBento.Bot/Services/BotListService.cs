using System.Diagnostics;
using System.Text;
using System.Text.Json;
using dotBento.Bot.Models;
using Microsoft.Extensions.Options;
using Serilog;

namespace dotBento.Bot.Services;

public class BotListService(HttpClient httpClient, IOptions<BotEnvConfig> options, Discord.WebSocket.DiscordSocketClient client)
{
    private readonly BotEnvConfig _config = options.Value;

    internal static Dictionary<string, object> ConfiguredProviders(BotListConfig? config)
    {
        var providers = new Dictionary<string, object>();
        if (config is null) return providers;
        foreach (var (name, token) in new[]
        {
            ("top.gg", config.TopGgApiToken),
            ("discord.bots.gg", config.DiscordBotsGgToken),
            ("discordbotlist.com", config.DiscordBotListToken)
        })
            if (!string.IsNullOrWhiteSpace(token)) providers.Add(name, token);
        return providers;
    }

    internal static Dictionary<string, object> BuildPayload(int guildCount, ulong botId, Dictionary<string, object> providers)
    {
        var payload = new Dictionary<string, object>(providers)
        {
            ["server_count"] = guildCount,
            ["bot_id"] = botId.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
        return payload;
    }

    public async Task UpdateBotLists(int guildCount)
    {
        var providers = ConfiguredProviders(_config.BotLists);
        if (providers.Count == 0)
        {
            return;
        }

        var currentProcess = Process.GetCurrentProcess();
        var startTime = DateTime.Now - currentProcess.StartTime;

        if (startTime.TotalMinutes <= 30)
        {
            Log.Information($"Skipping {nameof(UpdateBotLists)} because bot only just started");
            return;
        }

        Log.Information($"{nameof(UpdateBotLists)}: Starting");
        const string requestUri = "https://botblock.org/api/count";

        var postData = BuildPayload(guildCount, client.CurrentUser.Id, providers);

        var json = JsonSerializer.Serialize(postData);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await httpClient.PostAsync(requestUri, content);
        Log.Information(response.IsSuccessStatusCode
            ? $"{nameof(UpdateBotLists)}: Updated successfully"
            : $"{nameof(UpdateBotLists)}: Failed to post data. Status code: {response.StatusCode}");
    }
}
