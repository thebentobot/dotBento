using Fergun.Interactive;
using Fergun.Interactive.Selection;

namespace dotBento.Bot.Models.Discord.MultiSelect;

public class MultiSelectBuilder<T> : BaseSelectionBuilder<MultiSelect<T>, MultiSelectOption<T>, MultiSelectBuilder<T>>
{
    public override InputType InputType => InputType.SelectMenus;

    public override MultiSelect<T> Build() => new(this);
}