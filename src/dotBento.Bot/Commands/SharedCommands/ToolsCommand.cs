using Discord;
using dotBento.Bot.Enums;
using dotBento.Bot.Models;
using dotBento.Bot.Models.Discord;
using dotBento.Infrastructure.Commands;
using dotBento.Infrastructure.Utilities;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp.ColorSpaces.Conversion;
using SixLabors.ImageSharp.PixelFormats;

namespace dotBento.Bot.Commands.SharedCommands;

public class ToolsCommand(ImageCommands imageCommands, IOptions<BotEnvConfig> botEnvConfig, StylingUtilities stylingUtilities)
{
    public async Task<ResponseModel> GetColour(string colour)
    {
        var embed = new ResponseModel{ ResponseType = ResponseType.ImageWithEmbed };
        var colourImage = await imageCommands.GetColour(botEnvConfig.Value.ImageServer.ImageServerHost, colour);
        
        if (colourImage.IsFailure)
        {
            embed.ResponseType = ResponseType.Embed;
            embed.Embed.WithTitle("Error")
                .WithDescription(colourImage.Error)
                .WithColor(Color.Red);
            return embed;
        }
        
        embed.Stream = colourImage.Value.Image;
        embed.FileName = "colour.png";

        if (colourImage.Value.IsHex)
        {
            var (r, g, b) = HexToRgb(colour);
        
            embed.Embed
                .WithTitle($"Colour `{(colourImage.Value.IsHex ? colour : $"{r},{g},{b}")}`")
                .WithFooter($"{(colourImage.Value.IsHex ? $"RGB: {HexToRgb(colour)}" : $"Hex: {RgbToHex([r, g, b])}")} | HSV: {RgbToHsv(r, g, b)}")
                .WithImageUrl($"attachment://colour.png")
                .WithColor(new Color(Convert.ToUInt32(colour.Replace("#", ""), 16)));
        
            return embed;   
        }
        else
        {
            var (r, g, b) = RgbStringToRgb(colour);
            embed.Embed
                .WithTitle($"Colour `{colour}`")
                .WithFooter($"Hex: #{RgbToHex([r, g, b])} | HSV: {RgbToHsv(r, g, b)}")
                .WithImageUrl($"attachment://colour.png")
                .WithColor(new Color(r, g, b));
        
            return embed;
        }
    }
    
    public async Task<ResponseModel> GetDominantColour(string url)
    {
        var embed = new ResponseModel{ ResponseType = ResponseType.ImageWithEmbed };
        var getDominantColorAsync = await stylingUtilities.TryGetDominantColorAsync(url);
        
        if (getDominantColorAsync.IsFailure)
        {
            embed.ResponseType = ResponseType.Embed;
            embed.Embed
                .WithTitle("Error")
                .WithDescription($"Could not get the dominant colour by your provided input: `{url}`")
                .WithColor(Color.Red);
            return embed;
        }
        
        var dominantColor = getDominantColorAsync.Value;
        
        var hexColor = $"#{dominantColor.R:X2}{dominantColor.G:X2}{dominantColor.B:X2}";
        var rgbColor = $"{dominantColor.R},{dominantColor.G},{dominantColor.B}";
        var hsvColor = RgbToHsv(dominantColor.R, dominantColor.G, dominantColor.B);
        
        var colourImage = await imageCommands.GetColour(botEnvConfig.Value.ImageServer.ImageServerHost, hexColor);
        
        if (colourImage.IsFailure)
        {
            embed.ResponseType = ResponseType.Embed;
            embed.Embed.WithTitle("Error")
                .WithDescription(colourImage.Error)
                .WithColor(Color.Red);
            return embed;
        }
        
        embed.Stream = colourImage.Value.Image;
        embed.FileName = "colour.png";
        
        embed.Embed
            .WithTitle("Dominant Colour")
            .WithFooter($"Hex: {hexColor} | RGB: {rgbColor} | HSV: {hsvColor}")
            .WithImageUrl($"attachment://colour.png")
            .WithColor(dominantColor);
        
        return embed;
    }

    private static (int R, int G, int B) HexToRgb(string hexColor)
    {
        hexColor = hexColor.Replace("#", "");
    
        var r = int.Parse(hexColor.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        var g = int.Parse(hexColor.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        var b = int.Parse(hexColor.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
    
        return (r, g, b);
    }

    private static (float H, float S, float V) RgbToHsv(int r, int g, int b)
    {
        var color = new Rgba32((byte)r, (byte)g, (byte)b);
    
        var hsv = ColorSpaceConverter.ToHsv(color);
    
        return ((float)Math.Round(hsv.H), (float)Math.Round(hsv.S * 100), (float)Math.Round(hsv.V * 100));
    }

    private static string RgbToHex(int[] rgb)
    {
        return rgb.Select(component => component.ToString("X2")).Aggregate((a, b) => a + b);
    }
    
    private static (int R, int G, int B) RgbStringToRgb(string rgb)
    {
        var rgbArray = rgb.Split(',');
        return (int.Parse(rgbArray[0]), int.Parse(rgbArray[1]), int.Parse(rgbArray[2]));
    }
}