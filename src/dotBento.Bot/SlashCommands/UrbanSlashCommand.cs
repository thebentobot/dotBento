using System.Text.RegularExpressions;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Services;
using dotBento.EntityFramework.Context;
using dotBento.Infrastructure.Utilities;
using Fergun.Interactive;

namespace dotBento.Bot.SlashCommands;

public class UrbanSlashCommand(BotDbContext botDbContext, InteractiveService interactiveService, StylingUtilities stylingUtilities, UrbanDictionaryService urbanDictionaryService)
    : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("urban", "Search Urban Dictionary for a term")]
    public async Task UrbanCommand([Summary("define", "What do you want defined?")] string query,
        [Summary("hide", "Only show user info for you")] bool? hide = false)
    {
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        var urbanResult = await urbanDictionaryService.GetDefinition(query);
        if (urbanResult.HasNoValue)
        {
            embed.Embed.WithTitle($"No results found for {query}")
                .WithColor(Color.Red);
            await Context.SendResponse(interactiveService, embed, hide ?? false);
            return;
        }
        var result = urbanResult.Value.List.First();
        var urbanDictionaryColour = new Color(0x1c9fea);
        var embedAuthor = new EmbedAuthorBuilder()
            .WithName("Urban Dictionary")
            .WithUrl("https://www.urbandictionary.com/")
            .WithIconUrl("https://is4-ssl.mzstatic.com/image/thumb/Purple111/v4/81/c8/5a/81c85a6c-9f9d-c895-7361-0b19b3e5422e/mzl.gpzumtgx.png/246x0w.png");
        embed.Embed
            .WithAuthor(embedAuthor)
            .WithColor(urbanDictionaryColour)
            .WithTitle($"{query}")
            .WithUrl(result.Permalink)
            .WithDescription($"{ReplaceWithMarkdownLinks(result.Definition)}\n\n\"{ReplaceWithMarkdownLinks(result.Example)}\"")
            .AddField("Author", result.Author, true)
            .AddField("Rating", $"{result.ThumbsUp} \u2b06\ufe0f {result.ThumbsDown} \u2b07\ufe0f", true)
            .AddField("Created on", $"<t:{result.WrittenOn.ToUnixTimeSeconds()}:F>", true);
        
        await Context.SendResponse(interactiveService, embed, hide ?? false);
    }
    
    private static string ReplaceWithMarkdownLinks(string str) =>
        Regex.Replace(str,
            @"\[(.*?)\]",
            m =>
                $"[{m.Groups[1].Value}](https://www.urbandictionary.com/define.php?term={Uri.EscapeDataString(m.Groups[1].Value)})");
}