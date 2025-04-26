using dotBento.WebApi.Models;

namespace dotBento.WebApi.Configurations;

public static class ConfigData
{
    public static WebApiEnvConfig Data { get; private set; }
    
    static ConfigData()
    {
        Data = new WebApiEnvConfig
        {
            Environment = "",
            ApiKey = "CHANGE-ME-API-KEY",
            DatabaseConnectionString = "Host=localhost;Port=5432;Username=postgres;Password=password;Database=bento;Command Timeout=60;Timeout=60;Persist Security Info=True"
        };

        var configBuilder = new ConfigurationBuilder()
            .AddEnvironmentVariables();
        var config = configBuilder.Build();
        config.Bind(Data);
    }
}