using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;
using dotBento.Infrastructure.Dto;
using dotBento.Infrastructure.Services.Api;
using Ganss.Xss;

namespace dotBento.Infrastructure.Commands;

public sealed class ImageCommands(SushiiImageServerService sushiiImageServerService, HtmlSanitizer htmlSanitizer)
{
    public async Task<Result<ColourResponseDto>> GetColour(string imageServerHost, string colour)
    {
        string? hexColour = null;
        int[]? rgbColour = null;

        var hexMatch = Regex.Match(colour, @"^(?:#|0x)([0-9a-f]{6})$", RegexOptions.IgnoreCase);
        var rgbMatch = Regex.Match(colour, @"(^\d{1,3})\s*,?\s*(\d{1,3})\s*,?\s*(\d{1,3}$)", RegexOptions.IgnoreCase);

        if (!hexMatch.Success && !rgbMatch.Success)
        {
            return Result.Failure<ColourResponseDto>("Please provide a valid hexcode or RGB colour. Example: `#ff0000` or `255,0,0`");
        }
        else if (hexMatch.Success)
        {
            hexColour = hexMatch.Groups[1].Value;
            var match = Regex.Match(hexColour, @"([0-9a-f]{2})([0-9a-f]{2})([0-9a-f]{2})", RegexOptions.IgnoreCase);
            var red = int.Parse(match.Groups[1].Value, System.Globalization.NumberStyles.HexNumber);
            var green = int.Parse(match.Groups[2].Value, System.Globalization.NumberStyles.HexNumber);
            var blue = int.Parse(match.Groups[3].Value, System.Globalization.NumberStyles.HexNumber);
            rgbColour = new[] { red, green, blue };
        }
        else
        {
            if (rgbMatch.Success)
            {
                rgbColour =
                [
                    int.Parse(rgbMatch.Groups[1].Value), int.Parse(rgbMatch.Groups[2].Value),
                    int.Parse(rgbMatch.Groups[3].Value)
                ];
                hexColour = $"{RgbToHex(rgbColour[0])}{RgbToHex(rgbColour[1])}{RgbToHex(rgbColour[2])}";
            }
        }

        if (hexColour != null)
        {
            var hexValue = int.Parse(hexColour, System.Globalization.NumberStyles.HexNumber);
            if (hexValue is < 0 or > 16777215)
            {
                return Result.Failure<ColourResponseDto>("Please provide a valid hexcode, e.g. `#ff0000`");
            }
        }

        if (rgbColour != null && rgbColour.Any(component => component is < 0 or > 255))
        {
            return Result.Failure<ColourResponseDto>("Please provide a valid RGB colour, e.g. `255,0,0`");
        }
        
        var sanitizedHtml = htmlSanitizer.Sanitize($"<html><style>*{{margin:0;padding:0;}}</style><div style=\"background-color:{(hexColour != null ? $"#{hexColour}" : rgbColour)}; width:200px; height:200px\"></div></html>");

        var image = await sushiiImageServerService.GetSushiiImage(imageServerHost, sanitizedHtml, 200, 200);
        
        var result = image.IsSuccess ? new ColourResponseDto(image.Value, hexMatch.Success) : new ColourResponseDto(Stream.Null, false);
        
        return image.IsFailure ? Result.Failure<ColourResponseDto>("Could not get image from Sushii Image Server") : Result.Success(result);
    }
    
    private string RgbToHex(int colour)
    {
        return colour.ToString("X2");
    }
}