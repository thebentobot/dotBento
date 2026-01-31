namespace dotBento.Bot.Models;

public sealed class BotEnvConfig
{
    public string Environment { get; set; } = string.Empty;

    public DiscordConfig Discord { get; set; } = new DiscordConfig();
    
    public PrometheusConfig Prometheus { get; set; } = new PrometheusConfig();

    public DatabaseConfig PostgreSql { get; set; } = new DatabaseConfig();
    
    public BotConfig Bot { get; set; } = new BotConfig();

    public BotListConfig BotLists { get; set; } = new BotListConfig();
    
    public string OpenWeatherApiKey { get; set; } = string.Empty;
    public string LastFmApiKey { get; set; } = string.Empty;
    
    public SpotifyConfig Spotify { get; set; } = new SpotifyConfig();
    
    public ImageServerConfig ImageServer { get; set; } = new ImageServerConfig();
    
    public string LokiUrl { get; set; } = string.Empty;
}

public sealed class SpotifyConfig
{
    public string Key { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
}

public sealed class PrometheusConfig
{
    public string MetricsPusherEndpoint { get; set; } = string.Empty;
    public string MetricsPusherName { get; set; } = string.Empty;
}

public sealed class DiscordConfig
{
    public string Token { get; set; } = string.Empty;
    
    public string LogWebhookId { get; set; } = string.Empty;
    
    public string LogWebhookToken { get; set; } = string.Empty;
}

public sealed class DatabaseConfig
{
    public string ConnectionString { get; set; } = string.Empty;
}

public sealed class BotConfig
{
    public string Prefix { get; set; } = string.Empty;

    public ulong BaseServerId { get; set; }

    public ulong AnnouncementChannelId { get; set; }

    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Discord channel ID for sending bot logs. Set to 0 to disable.
    /// Environment variable: Bot__LogChannelId
    /// </summary>
    public ulong LogChannelId { get; set; }
}

public sealed class BotListConfig
{
    public string TopGgApiToken { get; set; } = string.Empty;
    public string DiscordBotsGgToken { get; set; } = string.Empty;
    public string DiscordBotListToken { get; set; } = string.Empty;
}

public sealed class ImageServerConfig
{
    public string Url { get; set; } = string.Empty;
}