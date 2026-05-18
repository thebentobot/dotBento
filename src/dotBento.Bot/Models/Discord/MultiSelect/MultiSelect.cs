using NetCord.Rest;
using Fergun.Interactive.Selection;

namespace dotBento.Bot.Models.Discord.MultiSelect;

public sealed class MultiSelect<T>(MultiSelectBuilder<T> builder) : BaseSelection<MultiSelectOption>(builder)
{
    public override List<IMessageComponentProperties> GetOrAddComponents(bool disableAll, List<IMessageComponentProperties>? components = null)
    {
        components ??= new List<IMessageComponentProperties>();
        var selectMenus = new Dictionary<int, (string customId, bool disabled, List<StringMenuSelectOptionProperties> options)>();

        foreach (var option in Options)
        {
            if (!selectMenus.TryGetValue(option.Row, out var menuData))
            {
                menuData = ($"selectmenu{option.Row}", disableAll, new List<StringMenuSelectOptionProperties>());
                selectMenus[option.Row] = menuData;
            }

            var selectOption = new StringMenuSelectOptionProperties(option.Option, option.Value)
                .WithDescription(option.Description)
                .WithDefault(option.IsDefault);
            menuData.options.Add(selectOption);
            selectMenus[option.Row] = menuData;
        }

        foreach (var (_, menuData) in selectMenus)
        {
            var selectMenu = new StringMenuProperties(menuData.customId, menuData.options)
                .WithDisabled(menuData.disabled);
            components.Add(selectMenu);
        }

        return components;
    }
}
