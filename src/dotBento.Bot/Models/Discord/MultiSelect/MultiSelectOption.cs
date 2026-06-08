namespace dotBento.Bot.Models.NetCord.MultiSelect;

public sealed record MultiSelectOption(
    string Option,
    string Value,
    int Row,
    string Description,
    bool IsDefault = false);