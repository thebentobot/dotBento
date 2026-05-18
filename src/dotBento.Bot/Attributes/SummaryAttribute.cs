namespace dotBento.Bot.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter)]
public sealed class SummaryAttribute(string summary) : Attribute
{
    public string Summary { get; } = summary;
}
