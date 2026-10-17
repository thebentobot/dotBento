using Discord;
using Discord.Interactions;
using Discord.Interactions.Builders;
using dotBento.Bot.Attributes;
using dotBento.Bot.Commands.SlashCommands;

namespace dotBento.Bot.Services;

public abstract class ConfiguredInteractionModule<T>(BotPresentation? presentation = null)
    : InteractionModuleBase<T> where T : class, IInteractionContext
{
    protected BotPresentation Presentation { get; } = presentation ?? new();

    public override void Construct(ModuleBuilder builder, InteractionService commandService)
    {
        base.Construct(builder, commandService);
        var brand = Presentation.Brand;
        var type = GetType();
        if (type == typeof(LeaderboardSlashCommand.BentoGroup))
        {
            builder.WithGroupName(brand.GiftLeaderboardName);
            builder.WithDescription($"{brand.GiftPluralName} leaderboards");
        }
        else if (builder.Description is { } description)
            builder.WithDescription(Describe(description));

        foreach (var command in builder.SlashCommands)
        {
            var originalName = command.Name;
            command.WithDescription(Describe(command.Description));
            if (type == typeof(BentoSlashCommand))
            {
                command.WithAttributes(new CanonicalCommandIdAttribute("bento"));
                command.WithName(brand.GiftCommandName);
            }
            else if (type == typeof(AboutSlashCommand))
            {
                command.WithAttributes(new CanonicalCommandIdAttribute("about:bento"));
                command.WithName(brand.AboutCommandName);
            }
            else if (type == typeof(LeaderboardSlashCommand.BentoGroup))
                command.WithAttributes(new CanonicalCommandIdAttribute($"leaderboard:bento:{originalName}"));
        }
    }

    private string Describe(string text) => System.Text.RegularExpressions.Regex.Replace(
        text, "Bento Box|Bento|bento", match => match.Value switch
        {
            "Bento Box" => Presentation.Brand.GiftName,
            "Bento" => Presentation.Brand.Name,
            _ => Presentation.Brand.Name.ToLowerInvariant()
        });
}
