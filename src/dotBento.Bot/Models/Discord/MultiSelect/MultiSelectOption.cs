namespace dotBento.Bot.Models.Discord.MultiSelect;

public record MultiSelectOption(
    string Option,
    string Value,
    int Row,
    string Description,
    bool IsDefault = false);