namespace dotBento.Bot.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
// ReSharper disable once ClassNeverInstantiated.Global
public sealed class ExamplesAttribute(params string[] examples) : Attribute
{
    public string[] Examples { get; } = examples;
}