namespace dotBento.Bot.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
// ReSharper disable once ClassNeverInstantiated.Global
public sealed class OptionsAttribute(params string[] options) : Attribute
{
    public string[] Options { get; } = options;
}