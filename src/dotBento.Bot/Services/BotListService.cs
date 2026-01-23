using System.Diagnostics;
using System.Text;
using System.Text.Json;
using dotBento.Bot.Models;
using Microsoft.Extensions.Options;
using Serilog;

namespace dotBento.Bot.Services;

public class BotListService(HttpClient httpClient, IOptions<BotEnvConfig> options)
{
    private readonly BotEnvConfig _config = options.Value;

    public async Task UpdateBotLists(int guildCount)
    {
        if (string.IsNullOrEmpty(_config.BotLists?.TopGgApiToken) ||
            string.IsNullOrEmpty(_config.BotLists?.DiscordBotListToken) ||
            string.IsNullOrEmpty(_config.BotLists?.DiscordBotsGgToken))
        {
            return;
        }

        var currentProcess = Process.GetCurrentProcess();
        var startTime = DateTime.Now - currentProcess.StartTime;

        if (startTime.Minutes <= 30)
        {
            Log.Information($"Skipping {nameof(UpdateBotLists)} because bot only just started");
            return;
        }

        Log.Information($"{nameof(UpdateBotLists)}: Starting");
        const string requestUri = "https://botblock.org/api/count";

        var postData = new Dictionary<string, object>
        {
            { "server_count",  guildCount },
            { "bot_id", "787041583580184609" },
            { "top.gg", _config.BotLists.TopGgApiToken },
            { "discord.bots.gg", _config.BotLists.DiscordBotsGgToken },
            { "discordbotlist.com", _config.BotLists.DiscordBotListToken },
        };

        var json = JsonSerializer.Serialize(postData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync(requestUri, content);
        Log.Information(response.IsSuccessStatusCode
            ? $"{nameof(UpdateBotLists)}: Updated successfully"
            : $"{nameof(UpdateBotLists)}: Failed to post data. Status code: {response.StatusCode}");
    }
}