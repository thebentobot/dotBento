namespace dotBento.Bot.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
// ReSharper disable once ClassNeverInstantiated.Global
public class ExamplesAttribute(params string[] examples) : Attribute
{
    public string[] Examples { get; } = examples;
}