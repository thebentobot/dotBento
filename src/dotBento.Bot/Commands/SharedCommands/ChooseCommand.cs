using NetCord;
using dotBento.Bot.Enums;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Resources;

namespace dotBento.Bot.Commands.SharedCommands;

public static class ChooseCommand
{
    public static Task<ResponseModel> Command(string options)
    {
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };

        if (!options.Contains(','))
        {
            embed.Embed.WithTitle("You need to separate options with commas")
                .WithColor(new Color(0xFF0000));
            return Task.FromResult(embed);
        }
        var listOfOptions = options.Trim().Split(",").ToList();

        switch (listOfOptions.Count)
        {
            case < 2:
            {
                embed.Embed.WithTitle("You need to provide at least 2 options")
                    .WithDescription($"Well obviously the choice is **{listOfOptions[0]}**, but perhaps you wanted me to choose between a few more options other than one? 🙄")
                    .WithColor(new Color(0xFF0000));
                break;
            }
            case > 20:
            {
                embed.Embed.WithTitle("You need to provide less than 20 options")
                    .WithDescription($"I can't choose between {listOfOptions.Count} options, that's too many! 😱")
                    .WithColor(new Color(0xFF0000));
                break;
            }
            default:
            {
                embed.Embed.WithTitle("I choose...")
                    .WithDescription($"**{ChooseOption(listOfOptions)}**")
                    .WithColor(DiscordConstants.BentoYellow);
                break;
            }
        }
        return Task.FromResult(embed);
    }

    private static string ChooseOption(IReadOnlyList<string> options)
    {
        var rnd = new Random();
        return options[rnd.Next(options.Count)];
    }
}
