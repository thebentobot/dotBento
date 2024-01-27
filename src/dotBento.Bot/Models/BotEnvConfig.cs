namespace dotBento.Bot.Models;

public class BotEnvConfig
{
    public string Environment { get; set; } = string.Empty;

    public DiscordConfig Discord { get; set; } = new DiscordConfig();
    
    public PrometheusConfig Prometheus { get; set; } = new PrometheusConfig();

    public DatabaseConfig PostgreSql { get; set; } = new DatabaseConfig();
    
    public BotConfig Bot { get; set; } = new BotConfig();

    public BotListConfig BotLists { get; set; } = new BotListConfig();
    
    public string OpenWeatherApiKey { get; set; } = string.Empty;
}

public class PrometheusConfig
{
    public string MetricsPusherEndpoint { get; set; } = string.Empty;
    public string MetricsPusherName { get; set; } = string.Empty;
}

public class DiscordConfig
{
    public string Token { get; set; } = string.Empty;
    
    public string LogWebhookId { get; set; } = string.Empty;
    
    public string LogWebhookToken { get; set; } = string.Empty;
}

public class DatabaseConfig
{
    public string ConnectionString { get; set; } = string.Empty;
}

public class BotConfig
{
    public string Prefix { get; set; } = string.Empty;

    public ulong BaseServerId { get; set; }

    public ulong AnnouncementChannelId { get; set; }

    public string Status { get; set; } = string.Empty;
}

public class BotListConfig
{
    public string TopGgApiToken { get; set; } = string.Empty;
}