namespace dotBento.Bot.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public sealed class CanonicalCommandIdAttribute(string id) : Attribute
{
    public string Id { get; } = id;
}
