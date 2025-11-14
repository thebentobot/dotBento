using System.Diagnostics;
using System.Text;
using System.Text.Json;
using dotBento.Bot.Configurations;
using Serilog;

namespace dotBento.Bot.Services;

public class BotListService(HttpClient httpClient)
{
    public async Task UpdateBotLists(int guildCount)
    {
        if (ConfigData.Data.BotLists?.TopGgApiToken == null ||
            ConfigData.Data.BotLists?.DiscordBotListToken == null ||
            ConfigData.Data.BotLists?.DiscordBotsGgToken == null)
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
            { "top.gg", ConfigData.Data.BotLists.TopGgApiToken },
            { "discord.bots.gg", ConfigData.Data.BotLists.DiscordBotsGgToken },
            { "discordbotlist.com", ConfigData.Data.BotLists.DiscordBotListToken },
        };

        var json = JsonSerializer.Serialize(postData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync(requestUri, content);
        Log.Information(response.IsSuccessStatusCode
            ? $"{nameof(UpdateBotLists)}: Updated successfully"
            : $"{nameof(UpdateBotLists)}: Failed to post data. Status code: {response.StatusCode}");
    }
}