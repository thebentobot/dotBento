using CSharpFunctionalExtensions;
using dotBento.Infrastructure.Extensions;
using SkiaSharp;
using Color = System.Drawing.Color;

namespace dotBento.Infrastructure.Utilities;

public sealed class StylingUtilities(HttpClient httpClient)
{
    public async Task<Discord.Color> GetDominantColorAsync(string imageUrl)
    {
        await using var stream = await httpClient.GetStreamAsync(imageUrl);
        using var bitmap = SKBitmap.Decode(stream)
            ?? throw new InvalidOperationException("Could not decode image stream.");
        return CalculateDominantColor(bitmap).ColorToDiscordColor();
    }

    public async Task<Result<Discord.Color>> TryGetDominantColorAsync(string imageUrl)
    {
        try
        {
            return await GetDominantColorAsync(imageUrl);
        }
        catch (Exception e)
        {
            return Result.Failure<Discord.Color>(e.Message);
        }
    }

    internal static Color CalculateDominantColor(SKBitmap bitmap)
    {
        double r = 0, g = 0, b = 0;
        var total = 0;

        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                r += pixel.Red;
                g += pixel.Green;
                b += pixel.Blue;
                total++;
            }
        }

        return Color.FromArgb((int)(r / total), (int)(g / total), (int)(b / total));
    }
}
