using Discord;
using Discord.Commands;
using Discord.WebSocket;
using dotBento.Bot.Attributes;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Resources;
using Fergun.Interactive;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.TextCommands;

[Name("Choose")]
public class ChooseTextCommand(
    IOptions<BotEnvConfig> botSettings,
    InteractiveService interactiveService) : BaseCommandModule(botSettings)
{
    [Command("choose", RunMode = RunMode.Async)]
    [Summary("List the options and get a choice. Separate options with commas, but write them in one e.g. 'option1, option2, option3'")]
    public async Task ChooseCommand([Remainder] [Summary("List of options to choose between")] string options)
    {
        _ = Context.Channel.TriggerTypingAsync();
        if (!options.Contains(','))
        {
            var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
            embed.Embed.WithTitle("You need to separate options with commas")
                .WithColor(Color.Red);
            await Context.SendResponse(interactiveService, embed);
        }
        var listOfOptions = options.Split(", ").ToList();
        
        switch (listOfOptions.Count)
        {
            case < 2:
            {
                var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
                embed.Embed.WithTitle("You need to provide at least 2 options")
                    .WithDescription($"Obviously it is **{listOfOptions[0]}**. I mean, what else could it be? 🤷‍♂️")
                    .WithColor(Color.Red);
                await Context.SendResponse(interactiveService, embed);
                break;
            }
            case > 20:
            {
                var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
                embed.Embed.WithTitle("You need to provide less than 20 options")
                    .WithDescription($"I can't choose between {listOfOptions.Count} options, that's too many! 😱")
                    .WithColor(Color.Red);
                await Context.SendResponse(interactiveService, embed);
                break;
            }
            default:
            {
                var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
                embed.Embed.WithTitle("I choose...")
                    .WithDescription($"**{ChooseOption(listOfOptions)}**")
                    .WithColor(DiscordConstants.BentoYellow);
                await Context.SendResponse(interactiveService, embed);
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