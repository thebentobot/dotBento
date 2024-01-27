using System.Text.RegularExpressions;
using Discord;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models.Discord;
using dotBento.Infrastructure.Services.Api;

namespace dotBento.Bot.Commands.SharedCommands;

public class UrbanCommand(UrbanDictionaryService urbanDictionaryService)
{
    public async Task<ResponseModel> Command(string query)
    {
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        var embedAuthor = new EmbedAuthorBuilder()
            .WithName("Urban Dictionary")
            .WithUrl("https://www.urbandictionary.com/")
            .WithIconUrl("https://is4-ssl.mzstatic.com/image/thumb/Purple111/v4/81/c8/5a/81c85a6c-9f9d-c895-7361-0b19b3e5422e/mzl.gpzumtgx.png/246x0w.png");
        var urbanResult = await urbanDictionaryService.GetDefinition(query);
        var urbanList = urbanResult?.List;
        if (urbanList == null || urbanList.Count == 0)
        {
            embed.Embed.WithTitle($"No results found for \"{query}\"")
                .WithColor(Color.Red)
                .WithAuthor(embedAuthor);
            return embed;
        }

        var result = urbanList.First();
        var urbanDictionaryColour = new Color(0x1c9fea);
        var urbanDictionaryDefinitionDescription = $"{ReplaceWithMarkdownLinks(result.Definition)}\n\n\"{ReplaceWithMarkdownLinks(result.Example)}\"";
        embed.Embed
            .WithAuthor(embedAuthor)
            .WithColor(urbanDictionaryColour)
            .WithTitle($"{query}")
            .WithUrl(result.Permalink)
            .WithDescription(urbanDictionaryDefinitionDescription.TrimToMaxLength(4096))
            .AddField("Author", result.Author, true)
            .AddField("Rating", $"{result.ThumbsUp} \u2b06\ufe0f {result.ThumbsDown} \u2b07\ufe0f", true)
            .AddField("Created on", $"<t:{result.WrittenOn.ToUnixTimeSeconds()}:F>", true);
            
        return embed;
    }
    
    private static string ReplaceWithMarkdownLinks(string str) =>
        Regex.Replace(str,
            @"\[(.*?)\]",
            m =>
                $"[{m.Groups[1].Value}](https://www.urbandictionary.com/define.php?term={Uri.EscapeDataString(m.Groups[1].Value)})");
}