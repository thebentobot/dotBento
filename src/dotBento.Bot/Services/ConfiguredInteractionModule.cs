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
}
