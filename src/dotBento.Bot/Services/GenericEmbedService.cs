using System.Text;
using Discord;
using Discord.Commands;
using dotBento.Bot.Attributes;
using dotBento.Bot.Resources;

namespace dotBento.Bot.Services;

public static class GenericEmbedService
{
    public static void HelpResponse(this EmbedBuilder embed, CommandInfo commandInfo, string prefix, string username)
    {
        embed.WithColor(DiscordConstants.InformationColorBlue);
        embed.WithTitle($"Information about '{prefix}{commandInfo.Name}' for {username}");
        embed.WithFooter("<> = required, [] = optional");

        if (!string.IsNullOrWhiteSpace(commandInfo.Summary))
        {
            embed.WithDescription(commandInfo.Summary.Replace("{{prefix}}", prefix));
        }

        var options = commandInfo.Attributes.OfType<OptionsAttribute>()
            .FirstOrDefault();
        if (options?.Options != null && options.Options.Any())
        {
            var optionsString = new StringBuilder();
            foreach (var option in options.Options)
            {
                optionsString.AppendLine($"- {option}");
            }

            embed.AddField("Options", optionsString.ToString());
        }

        var examples = commandInfo.Attributes.OfType<ExamplesAttribute>()
            .FirstOrDefault();
        if (examples?.Examples != null && examples.Examples.Any())
        {
            var examplesString = new StringBuilder();
            foreach (var example in examples.Examples)
            {
                examplesString.AppendLine($"`{prefix}{example}`");
            }

            embed.AddField("Examples", examplesString.ToString());
        }

        var aliases = commandInfo.Aliases.Where(a => a != commandInfo.Name).ToList();
        if (aliases.Any())
        {
            var aliasesString = new StringBuilder();
            for (var index = 0; index < aliases.Count; index++)
            {
                if (index != 0)
                {
                    aliasesString.Append(", ");
                }
                var alias = aliases[index];
                aliasesString.Append($"`{prefix}{alias}`");
            }

            embed.AddField("Aliases", aliasesString.ToString());
        }
    }
}