namespace Gavel.Contracts;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class IntegrationEventAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
