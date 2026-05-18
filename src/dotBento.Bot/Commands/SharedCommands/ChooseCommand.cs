using dotBento.Bot.Enums;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Resources;
using dotBento.Bot.Services;

namespace dotBento.Bot.Commands.SharedCommands;

public static class ChooseCommand
{
    public static Task<ResponseModel> Command(string options)
    {
        if (!options.Contains(','))
        {
            return Task.FromResult(GenericEmbedService.ErrorEmbed("You need to separate options with commas"));
        }
        var listOfOptions = options.Trim().Split(",").ToList();

        var embed = new ResponseModel { ResponseType = ResponseType.Embed };
        switch (listOfOptions.Count)
        {
            case < 2:
                return Task.FromResult(GenericEmbedService.ErrorEmbed("You need to provide at least 2 options",
                    $"Well obviously the choice is **{listOfOptions[0]}**, but perhaps you wanted me to choose between a few more options other than one? 🙄"));
            case > 20:
                return Task.FromResult(GenericEmbedService.ErrorEmbed("You need to provide less than 20 options",
                    $"I can't choose between {listOfOptions.Count} options, that's too many! 😱"));
            default:
                embed.Embed.WithTitle("I choose...")
                    .WithDescription($"**{ChooseOption(listOfOptions)}**")
                    .WithColor(DiscordConstants.BentoYellow);
                break;
        }
        return Task.FromResult(embed);
    }

    private static string ChooseOption(IReadOnlyList<string> options)
    {
        var rnd = new Random();
        return options[rnd.Next(options.Count)];
    }
}
