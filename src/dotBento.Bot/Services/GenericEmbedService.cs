using System.Text;
using NetCord.Rest;
using NetCord.Services.Commands;
using dotBento.Bot.Attributes;
using dotBento.Bot.Resources;

namespace dotBento.Bot.Services;

public static class GenericEmbedService
{
    public static void HelpResponse(this EmbedProperties embed, ICommandInfo<CommandContext> commandInfo, string prefix, string username)
    {
        var primaryAlias = commandInfo.Aliases.FirstOrDefault() ?? string.Empty;

        embed.WithColor(DiscordConstants.InformationColorBlue);
        embed.WithTitle($"Information about '{prefix}{primaryAlias}' for {username}");
        embed.WithFooter(new EmbedFooterProperties().WithText("<> = required, [] = optional"));

        var summaryAttribute = commandInfo.Attributes
            .GetValueOrDefault(typeof(SummaryAttribute))
            ?.OfType<SummaryAttribute>()
            .FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(summaryAttribute?.Summary))
        {
            embed.WithDescription(summaryAttribute.Summary.Replace("{{prefix}}", prefix));
        }

        var options = commandInfo.Attributes
            .GetValueOrDefault(typeof(OptionsAttribute))
            ?.OfType<OptionsAttribute>()
            .FirstOrDefault();
        if (options?.Options != null && options.Options.Any())
        {
            var optionsString = new StringBuilder();
            foreach (var option in options.Options)
            {
                optionsString.AppendLine($"- {option}");
            }

            embed.AddFields([new EmbedFieldProperties().WithName("Options").WithValue(optionsString.ToString())]);
        }

        var examples = commandInfo.Attributes
            .GetValueOrDefault(typeof(ExamplesAttribute))
            ?.OfType<ExamplesAttribute>()
            .FirstOrDefault();
        if (examples?.Examples != null && examples.Examples.Any())
        {
            var examplesString = new StringBuilder();
            foreach (var example in examples.Examples)
            {
                examplesString.AppendLine($"`{prefix}{example}`");
            }

            embed.AddFields([new EmbedFieldProperties().WithName("Examples").WithValue(examplesString.ToString())]);
        }

        var aliases = commandInfo.Aliases.Skip(1).ToList();
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

            embed.AddFields([new EmbedFieldProperties().WithName("Aliases").WithValue(aliasesString.ToString())]);
        }
    }
}
