using Discord;
using Discord.Interactions;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Resources;
using Fergun.Interactive;

namespace dotBento.Bot.SlashCommands;

[Group("choose", "Get help choosing something")]
public class ChooseSlashCommand(InteractiveService interactiveService) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("list", "Get Bento to choose from a list of options")]
    public async Task UserAvatarCommand(
        [Summary("options", "Write a list, separated by commas")] string options,
        [Summary("hide", "Only show the result for you")] bool? hide = false
    )
    {
        if (!options.Contains(','))
        {
            var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
            embed.Embed.WithTitle("You need to separate options with commas")
                .WithColor(Color.Red);
            await Context.SendResponse(interactiveService, embed, true);
        }
        var listOfOptions = options.Trim().Split(",").ToList();
        
        switch (listOfOptions.Count)
        {
            case < 2:
            {
                var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
                embed.Embed.WithTitle("You need to provide at least 2 options")
                    .WithDescription($"Well obviously the choice is **{listOfOptions[0]}**, but perhaps you wanted me to choose between a few more options other than one? 🙄")
                    .WithColor(Color.Red);
                await Context.SendResponse(interactiveService, embed, true);
                break;
            }
            case > 20:
            {
                var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
                embed.Embed.WithTitle("You need to provide less than 20 options")
                    .WithDescription($"I can't choose between {listOfOptions.Count} options, that's too many! 😱")
                    .WithColor(Color.Red);
                await Context.SendResponse(interactiveService, embed, true);
                break;
            }
            default:
            {
                var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
                embed.Embed.WithTitle("I choose...")
                    .WithDescription($"**{ChooseOption(listOfOptions)}**")
                    .WithColor(DiscordConstants.BentoYellow);
                await Context.SendResponse(interactiveService, embed, hide ?? false);
                break;
            }
        }
    }
    
    private static string ChooseOption(IReadOnlyList<string> options)
    {
        var rnd = new Random();
        return options[rnd.Next(options.Count)];
    }
}