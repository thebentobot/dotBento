using Discord;
using Fergun.Interactive.Selection;

namespace dotBento.Bot.Models.Discord.MultiSelect;

public class MultiSelect<T> : BaseSelection<MultiSelectOption<T>>
{
    public MultiSelect(MultiSelectBuilder<T> builder)
        : base(builder)
    {
    }

    public override ComponentBuilder GetOrAddComponents(bool disableAll, ComponentBuilder builder = null)
    {
        builder ??= new ComponentBuilder();
        var selectMenus = new Dictionary<int, SelectMenuBuilder>();

        foreach (var option in Options)
        {
            if (!selectMenus.ContainsKey(option.Row))
            {
                selectMenus[option.Row] = new SelectMenuBuilder()
                    .WithCustomId($"selectmenu{option.Row}")
                    .WithDisabled(disableAll);
            }

            var optionBuilder = new SelectMenuOptionBuilder()
                .WithLabel(option.Option)
                .WithValue(option.Value)
                .WithDescription(option.Description)
                .WithDefault(option.IsDefault);

            selectMenus[option.Row].AddOption(optionBuilder);
        }

        foreach ((int row, var selectMenu) in selectMenus)
        {
            builder.WithSelectMenu(selectMenu, row);
        }

        return builder;
    }
}