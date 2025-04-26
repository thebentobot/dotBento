namespace dotBento.WebApi.Models;

public sealed class WebApiEnvConfig
{
    public string Environment { get; set; } = string.Empty;
    
    public string ApiKey { get; set; } = string.Empty;
    
    public string DatabaseConnectionString { get; set; } = string.Empty;    
}

