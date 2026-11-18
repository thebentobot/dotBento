using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;
using dotBento.Infrastructure.Dto;
using dotBento.Infrastructure.Services.Api;
using Ganss.Xss;

namespace dotBento.Infrastructure.Commands;

public sealed class ImageCommands(ImageRendererService imageRendererService)
{
    /// <summary>
    /// Sanitizes the colour card before it is handed to the renderer. The
    /// allowlist is deliberately narrower than the HtmlSanitizer defaults: the
    /// card is a single styled <c>div</c> plus the stylesheet reset that
    /// removes the browser's default body margin. Widen it before reusing this
    /// profile for anything else.
    /// </summary>
    private static readonly HtmlSanitizer ColourCardSanitizer = CreateColourCardSanitizer();

    public async Task<Result<ColourResponseDto>> GetColour(string imageServerHost, string colour)
    {
        var hexMatch = Regex.Match(colour, @"^(?:#|0x)([0-9a-f]{6})$", RegexOptions.IgnoreCase);
        var rgbMatch = Regex.Match(colour, @"(^\d{1,3})\s*,?\s*(\d{1,3})\s*,?\s*(\d{1,3}$)", RegexOptions.IgnoreCase);

        if (!hexMatch.Success && !rgbMatch.Success)
        {
            return Result.Failure<ColourResponseDto>("Please provide a valid hexcode or RGB colour. Example: `#ff0000` or `255,0,0`");
        }

        var isHexInput = hexMatch.Success;
        string hexColour;

        if (isHexInput)
        {
            hexColour = hexMatch.Groups[1].Value;
        }
        else
        {
            var rgbColour = new[]
            {
                int.Parse(rgbMatch.Groups[1].Value), int.Parse(rgbMatch.Groups[2].Value),
                int.Parse(rgbMatch.Groups[3].Value)
            };

            if (rgbColour.Any(component => component is < 0 or > 255))
            {
                return Result.Failure<ColourResponseDto>("Please provide a valid RGB colour, e.g. `255,0,0`");
            }

            hexColour = string.Concat(rgbColour.Select(RgbToHex));
        }

        var sanitizedHtml = ColourCardSanitizer.Sanitize(
            $"<style>*{{margin:0;padding:0;}}</style><div style=\"background-color:#{hexColour};width:200px;height:200px\"></div>");

        var image = await imageRendererService.GetImage(imageServerHost, sanitizedHtml, 200, 200);

        var result = image.IsSuccess ? new ColourResponseDto(image.Value, isHexInput) : new ColourResponseDto(Stream.Null, false);

        return image.IsFailure ? Result.Failure<ColourResponseDto>("Could not get image from the image renderer") : Result.Success(result);
    }

    private static HtmlSanitizer CreateColourCardSanitizer()
    {
        var options = new HtmlSanitizerOptions();

        options.AllowedTags.Clear();
        options.AllowedTags.Add("div");

        // The reset has to survive sanitization. Without it the browser's
        // default body margin offsets the 200x200 card inside the 200x200
        // viewport. HtmlSanitizer still filters the rules inside the element.
        options.AllowedTags.Add("style");

        options.AllowedAttributes.Clear();
        options.AllowedAttributes.Add("style");

        return new HtmlSanitizer(options);
    }

    private static string RgbToHex(int colour)
    {
        return colour.ToString("X2");
    }
}
