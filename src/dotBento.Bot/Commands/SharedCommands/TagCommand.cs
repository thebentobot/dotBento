using Discord;
using dotBento.Bot.Enums;
using dotBento.Bot.Models.Discord;
using dotBento.Infrastructure.Commands;

namespace dotBento.Bot.Commands.SharedCommands;

public class TagCommand(TagCommands tagCommands)
{
    public async Task<ResponseModel> CreateTagAsync(long userId, long guildId, string name, string content)
    {
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        // we need to make content a record/class that has message content, which we definitely need to sanitize and then discord attachments separately which we can trust but would get caught by sanitization
        var result = await tagCommands.CreateTagAsync(userId, guildId, name, content);
        if (result.IsFailure)
        {
            embed.Embed
                .WithColor(Color.Red)
                .WithTitle("Error")
                .WithDescription(result.Error);
            return embed;
        }

        embed.Embed
            .WithColor(Color.Green)
            .WithTitle($"The tag \"{name}\" has been created successfully");
        return embed;
    }
}