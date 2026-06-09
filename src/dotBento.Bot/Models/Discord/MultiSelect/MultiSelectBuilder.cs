using Fergun.Interactive;
using Fergun.Interactive.Selection;

namespace dotBento.Bot.Models.Discord.MultiSelect;

public sealed class MultiSelectBuilder<T> : BaseSelectionBuilder<MultiSelect<T>, MultiSelectOption, MultiSelectBuilder<T>>
{
    public override InputType InputType => InputType.SelectMenus;

    public override MultiSelect<T> Build() => new(this);
}