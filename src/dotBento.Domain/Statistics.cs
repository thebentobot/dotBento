using Prometheus;

namespace dotBento.Domain;

// TODO: Make use of metrics
public static class Statistics
{
    public static readonly Gauge DiscordServerCount = Metrics
        .CreateGauge("discord_server_count", "Total count of all servers the bot is in");


    public static readonly Counter LastfmApiCalls = Metrics
        .CreateCounter("lastfm_api_calls", "Amount of last.fm API calls",
            new CounterConfiguration
            {
                LabelNames = new[] { "method" }
            });

    public static readonly Counter LastfmErrors = Metrics
        .CreateCounter("lastfm_errors", "Amount of errors Last.fm is returning");

    public static readonly Counter LastfmFailureErrors = Metrics
        .CreateCounter("lastfm_errors_failure", "Amount of failure errors Last.fm is returning");

    public static readonly Counter SpotifyApiCalls = Metrics
        .CreateCounter("spotify_api_calls", "Amount of Spotify API calls");
    
    public static readonly Counter SpotifyApiErrors = Metrics
        .CreateCounter("spotify_api_errors", "Amount of Spotify API errors");
    
    public static readonly Counter WeatherApiCalls = Metrics
        .CreateCounter("weather_api_calls", "Amount of Weather API calls");
    
    public static readonly Counter WeatherApiErrors = Metrics
        .CreateCounter("weather_api_errors", "Amount of Weather API errors");

    public static readonly Counter CommandsExecuted = Metrics
        .CreateCounter("bot_commands_executed", "Amount of commands executed",
            new CounterConfiguration
            {
                LabelNames = new[] { "name" }
            });

    public static readonly Counter SlashCommandsExecuted = Metrics
        .CreateCounter("bot_slash_commands_executed", "Amount of slash commands executed",
            new CounterConfiguration
            {
                LabelNames = new[] { "name" }
            });

    public static readonly Histogram TextCommandHandlerDuration = Metrics
        .CreateHistogram("bot_text_command_handler_duration", "Histogram of text command handler duration");

    public static readonly Histogram SlashCommandHandlerDuration = Metrics
        .CreateHistogram("bot_slash_command_handler_duration", "Histogram of text command handler duration");

    public static readonly Counter UserCommandsExecuted = Metrics
        .CreateCounter("bot_user_commands_executed", "Amount of user commands executed");

    public static readonly Counter AutoCompletesExecuted = Metrics
        .CreateCounter("bot_autocompletes_executed", "Amount of autocompletes executed");

    public static readonly Counter SelectMenusExecuted = Metrics
        .CreateCounter("bot_select_menus_executed", "Amount of selectmenus executed");

    public static readonly Counter ModalsExecuted = Metrics
        .CreateCounter("bot_modals_executed", "Amount of modals executed");

    public static readonly Counter ButtonExecuted = Metrics
        .CreateCounter("bot_button_executed", "Amount of buttons executed");
    
    public static readonly Counter DiscordEvents = Metrics
        .CreateCounter("bot_discord_events", "Amount of events through the Discord gateway",
            new CounterConfiguration
            {
                LabelNames = new[] { "name" }
            });
    
    public static readonly Gauge RegisteredUserCount = Metrics
        .CreateGauge("bot_registered_users_count", "Total count of all users in the database");
    
    public static readonly Gauge RegisteredDiscordUserCount = Metrics
        .CreateGauge("bot_registered_discord_users_count", "Total sum of all users aggregated by guild member count");

    public static readonly Gauge ActiveSupporterCount = Metrics
        .CreateGauge("bot_active_supporter_count", "Total count of all supporters in the database");

    public static readonly Gauge RegisteredGuildCount = Metrics
        .CreateGauge("bot_registered_guilds_count", "Total count of all guilds in the database");

    /*
    public static readonly Gauge OneDayActiveUserCount = Metrics
        .CreateGauge("bot_active_users_count_1d", "Total count of users who've used the bot in the last day");

    public static readonly Gauge SevenDayActiveUserCount = Metrics
        .CreateGauge("bot_active_users_count_7d", "Total count of users who've used the bot in the last 7 days");

    public static readonly Gauge ThirtyDayActiveUserCount = Metrics
        .CreateGauge("bot_active_users_count_30d", "Total count of users who've used the bot in the last 30 days");
    */

    public static readonly Counter CommandsFailed = Metrics
        .CreateCounter("bot_commands_failed", "Amount of commands that failed",
            new CounterConfiguration
            {
                LabelNames = new[] { "name" }
            });
    
    public static readonly Counter SlashCommandsFailed = Metrics
        .CreateCounter("bot_slash_commands_failed", "Amount of slash commands that failed",
            new CounterConfiguration
            {
                LabelNames = new[] { "name" }
            });
}