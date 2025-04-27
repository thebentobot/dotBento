using dotBento.Bot.Models;
using Microsoft.Extensions.Configuration;

namespace dotBento.Bot.Configurations;

public static class ConfigData
{
    public static BotEnvConfig Data { get; private set; }

    static ConfigData()
    {
        Data = new BotEnvConfig
        {
            PostgreSql = new DatabaseConfig
            {
                ConnectionString = "Host=localhost;Port=5432;Username=postgres;Password=password;Database=bento;Command Timeout=60;Timeout=60;Persist Security Info=True"
            },
            Bot = new BotConfig
            {
                AnnouncementChannelId = 0000000000000,
                Prefix = "?",
                Status = "We out here",
                BaseServerId = 0000000000000
            },
            Environment = "local",
            Discord = new DiscordConfig
            {
                Token = "CHANGE-ME-DISCORD-TOKEN",
                LogWebhookId = "CHANGE-ME-WEBHOOK-ID",
                LogWebhookToken = "CHANGE-ME-WEBHOOK-TOKEN"
            },
            Prometheus = new PrometheusConfig
            {
                MetricsPusherEndpoint = "",
                MetricsPusherName = ""
            },
            BotLists = new BotListConfig
            {
                TopGgApiToken = "CHANGE-ME-TOPGG-API-TOKEN"
            },
            OpenWeatherApiKey = "CHANGE-ME-OPENWEATHER-API-KEY",
            LastFmApiKey = "CHANGE-ME-LASTFM-API-KEY",
            Spotify = new SpotifyConfig
            {
                Key = "CHANGE-ME-SPOTIFY-KEY",
                Secret = "CHANGE-ME-SPOTIFY-SECRET"
            },
            ImageServer = new ImageServerConfig
            {
                ImageServerHost = "localhost"
            },
            LokiUrl = "http://localhost:3100"
        };

        var configBuilder = new ConfigurationBuilder()
            .AddEnvironmentVariables();
        var config = configBuilder.Build();
        config.Bind(Data);
    }
}