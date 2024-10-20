using Discord;
using Discord.Interactions;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models.Discord;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

[Group("tools", "Small tools for convenience")]
public class ToolsSlashCommand(InteractiveService interactiveService, ToolsCommand toolsCommand) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("colour", "Get the colour of a hex code or RGB")]
    public async Task GetColourCommand(
        [Summary("colour", "Hex code or RGB separated by commas")] string colour,
        [Summary("hide", "Only show colour for you")] bool? hide = false) =>
        await Context.SendResponse(interactiveService, await toolsCommand.GetColour(colour), hide ?? false);
    
    [SlashCommand("dominantcolour", "Get the dominant colour of an image, either by URL or by attachment")]
    public async Task GetDominantColourCommand(
        [Summary("url", "URL of the image")] string? url,
        [Summary("attachment", "Add a photo")] IAttachment? attachment = null,
        [Summary("hide", "Only show colour for you")] bool? hide = false)
    {
        if (attachment != null)
        {
            url = attachment.Url;
        }
        
        if (string.IsNullOrWhiteSpace(url))
        {
            await Context.SendResponse(interactiveService, ErrorEmbed("Please provide a URL or attach an image to the command."), hide ?? false);
            return;
        }
        
        await Context.SendResponse(interactiveService, await toolsCommand.GetDominantColour(url), hide ?? false);
    }
    
    private static ResponseModel ErrorEmbed(string error)
    {
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        embed.Embed.WithTitle(error)
            .WithColor(Color.Red);
        return embed;
    }
}