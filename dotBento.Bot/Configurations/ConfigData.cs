using System.Text.Json;
using dotBento.Bot.Models;

namespace dotBento.Bot.Configurations;

public static class ConfigData
{
    private const string ConfigFolder = "configs";
    private const string ConfigFile = "config.json";

    public static BotEnvConfig Data { get; }

    static ConfigData()
    {
        if (!Directory.Exists(ConfigFolder))
        {
            Directory.CreateDirectory(ConfigFolder);
        }

        if (!File.Exists(ConfigFolder + "/" + ConfigFile))
        {
            // Default config template
            Data = new BotEnvConfig
            {
                PostgreSQL =
                {
                    ConnectionString = "Host=localhost;Port=5432;Username=postgres;Password=password;Database=bento;Command Timeout=60;Timeout=60;Persist Security Info=True"
                },
                Bot =
                {
                    AnnouncementChannelId = 0000000000000,
                    Prefix = "_",
                    Status = "We out here",
                    BaseServerId = 0000000000000
                },
                Environment = "local",
                Discord =
                {
                    Token = "CHANGE-ME-DISCORD-TOKEN",
                    LogWebhookId = "CHANGE-ME-WEBHOOK-ID",
                    LogWebhookToken = "CHANGE-ME-WEBHOOK-TOKEN"
                },
                Prometheus =
                {
                    MetricsPusherEndpoint = "",
                    MetricsPusherName = "",
                },
                BotLists =
                {
                    TopGgApiToken = "CHANGE-ME-TOPGG-API-TOKEN",
                }
            };

            var json = JsonSerializer.Serialize(Data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(ConfigFolder, ConfigFile), json);

            Console.WriteLine($"Created new bot configuration file with default values. \nPlease set your API keys in {ConfigFolder}/{ConfigFile} before running the bot again. \n \nExiting in 10 seconds...", ConsoleColor.Red);

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