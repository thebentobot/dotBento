namespace dotBento.Bot.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
// ReSharper disable once ClassNeverInstantiated.Global
public class ExamplesAttribute : Attribute
{
    public string[] Examples { get; }

    public ExamplesAttribute(params string[] examples)
    {
        this.Examples = examples;
    }
}