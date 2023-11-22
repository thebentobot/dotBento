namespace dotBento.Bot.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
// ReSharper disable once ClassNeverInstantiated.Global
public class OptionsAttribute : Attribute
{
    public string[] Options { get; }

    public OptionsAttribute(params string[] options)
    {
        Options = options;
    }
}