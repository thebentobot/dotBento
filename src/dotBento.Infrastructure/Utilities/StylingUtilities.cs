using CSharpFunctionalExtensions;
using dotBento.Infrastructure.Extensions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = System.Drawing.Color;

namespace dotBento.Infrastructure.Utilities;

public class StylingUtilities(HttpClient httpClient)
{
    public async Task<Discord.Color> GetDominantColorAsync(string imageUrl)
    {
        using (var stream = await httpClient.GetStreamAsync(imageUrl))
        {
            using (var image = Image.Load<Rgba32>(stream))
            {
                return CalculateDominantColor(image).ColorToDiscordColor();
            }
        }
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

    private static Color CalculateDominantColor(Image<Rgba32> image)
    {
        double r = 0;
        double g = 0;
        double b = 0;
        var total = 0;

        for (var y = 0; y < image.Height; y++)
        {
            for (var x = 0; x < image.Width; x++)
            {
                var pixel = image[x, y];

                r += pixel.R;
                g += pixel.G;
                b += pixel.B;

                total++;
            }
        }

        return Color.FromArgb((int)(r / total), (int)(g / total), (int)(b / total));
    }
}