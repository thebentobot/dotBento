namespace dotBento.Domain;

public sealed class CacheConfig
{
    public string Prefix { get; set; } = "dotbento:";
}

public sealed class RemoteLoggingConfig
{
    public const int LokiQueueLimit = 1_000;
    public bool LokiEnabled { get; set; } = true;
}
