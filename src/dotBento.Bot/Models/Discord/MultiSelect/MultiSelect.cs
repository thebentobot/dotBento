using Discord;
using Fergun.Interactive.Selection;

namespace dotBento.Bot.Models.Discord.MultiSelect;

public sealed class MultiSelect<T>(MultiSelectBuilder<T> builder) : BaseSelection<MultiSelectOption>(builder)
{
    public override ComponentBuilder GetOrAddComponents(bool disableAll, ComponentBuilder? builder = null)
    {
        builder ??= new ComponentBuilder();
        var selectMenus = new Dictionary<int, SelectMenuBuilder>();

        foreach (var option in Options)
        {
            if (!selectMenus.TryGetValue(option.Row, out var value))
            {
                value = new SelectMenuBuilder()
                    .WithCustomId($"selectmenu{option.Row}")
                    .WithDisabled(disableAll);
                selectMenus[option.Row] = value;
            }

            var optionBuilder = new SelectMenuOptionBuilder()
                .WithLabel(option.Option)
                .WithValue(option.Value)
                .WithDescription(option.Description)
                .WithDefault(option.IsDefault);
            value.AddOption(optionBuilder);
        }

        foreach (var (row, selectMenu) in selectMenus)
        {
            builder.WithSelectMenu(selectMenu, row);
        }

        return builder;
    }
}