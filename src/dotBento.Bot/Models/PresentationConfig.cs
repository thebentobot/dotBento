namespace dotBento.Bot.Models;

public sealed class ThemeConfig
{
    public string Primary { get; set; } = "#FDE047";
    public string Success { get; set; } = "#32CD32";
    public string Warning { get; set; } = "#FFAE42";
    public string Error { get; set; } = "#FF0000";
    public string Information { get; set; } = "#448AFF";
}

public sealed class PaginationConfig
{
    public string First { get; set; } = "⏮️";
    public string Previous { get; set; } = "◀️";
    public string Next { get; set; } = "▶️";
    public string Last { get; set; } = "⏭️";
    public string Jump { get; set; } = "🔢";
}

public sealed class FeatureConfig
{
    public bool MediaEnabled { get; set; } = true;
    public bool HoroscopeEnabled { get; set; } = true;
}
