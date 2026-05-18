namespace dotBento.Bot.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ModuleNameAttribute(string moduleName) : Attribute
{
    public string ModuleName { get; } = moduleName;
}
