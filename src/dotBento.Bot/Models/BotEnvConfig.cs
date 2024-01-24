namespace dotBento.Bot.Models;

public class BotEnvConfig
{
    public string Environment { get; set; }

    public DiscordConfig Discord { get; set; }
    
    public PrometheusConfig Prometheus { get; set; }

    public DatabaseConfig PostgreSQL { get; set; }
    
    public BotConfig Bot { get; set; }

    public BotListConfig BotLists { get; set; }
    
    public string OpenWeatherApiKey { get; set; }
}

public class PrometheusConfig
{
    public string MetricsPusherEndpoint { get; set; }
    public string MetricsPusherName { get; set; }
}

public class DiscordConfig
{
    public string Token { get; set; }
    
    public string LogWebhookId { get; set; }
    
    public string LogWebhookToken { get; set; }
}

public class DatabaseConfig
{
    public string ConnectionString { get; set; }
}

public class BotConfig
{
    public string Prefix { get; set; }

    public ulong BaseServerId { get; set; }

    public ulong AnnouncementChannelId { get; set; }

    public string Status { get; set; }
}

public class BotListConfig
{
    public string TopGgApiToken { get; set; }
}