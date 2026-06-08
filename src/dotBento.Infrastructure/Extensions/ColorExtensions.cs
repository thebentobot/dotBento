using NetCord;

namespace dotBento.Infrastructure.Extensions;

public static class ColorExtensions
{
    public static Color ColorToNetCordColor(this System.Drawing.Color color) => new(color.R, color.G, color.B);

    public static Color ColorToDiscordColor(this System.Drawing.Color color) => color.ColorToNetCordColor();
}
