using System.Text.Json;
using dotBento.Bot.Models;

namespace dotBento.Bot.Configurations;

public static class ConfigData
{
    private const string ConfigFolder = "../../../configs";
    private const string ConfigFile = "config.json";

    // ReSharper disable once MemberCanBePrivate.Global
    public static BotEnvConfig Data { get; }

    static ConfigData()
    {
        if (!Directory.Exists(ConfigFolder))
        {
            Directory.CreateDirectory(ConfigFolder);
        }

        if (!File.Exists(ConfigFolder + "/" + ConfigFile))
        {
            Data = new BotEnvConfig
            {
                PostgreSQL = new DatabaseConfig(),
                Bot = new BotConfig(),
                Environment = "local",
                Discord = new DiscordConfig(),
                Prometheus = new PrometheusConfig(),
                BotLists = new BotListConfig(),
            };

            // Set default values after initializing the properties
            Data.PostgreSQL.ConnectionString = "Host=localhost;Port=5432;Username=postgres;Password=password;Database=bento;Command Timeout=60;Timeout=60;Persist Security Info=True";
            Data.Bot.AnnouncementChannelId = 0000000000000;
            Data.Bot.Prefix = "_";
            Data.Bot.Status = "We out here";
            Data.Bot.BaseServerId = 0000000000000;
            Data.Discord.Token = "CHANGE-ME-DISCORD-TOKEN";
            Data.Discord.LogWebhookId = "CHANGE-ME-WEBHOOK-ID";
            Data.Discord.LogWebhookToken = "CHANGE-ME-WEBHOOK-TOKEN";
            Data.Prometheus.MetricsPusherEndpoint = "";
            Data.Prometheus.MetricsPusherName = "";
            Data.BotLists.TopGgApiToken = "CHANGE-ME-TOPGG-API-TOKEN";

            var json = JsonSerializer.Serialize(Data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(ConfigFolder, ConfigFile), json);

            Console.WriteLine($"Created new bot configuration file with default values.\nPlease set your API keys in {ConfigFolder}/{ConfigFile} before running the bot again.\n\nExiting in 10 seconds...");

            Thread.Sleep(10000);
            Environment.Exit(0);
        }
        else
        {
            var json = File.ReadAllText(Path.Combine(ConfigFolder, ConfigFile));
            Data = JsonSerializer.Deserialize<BotEnvConfig>(json);
        }
    }
}