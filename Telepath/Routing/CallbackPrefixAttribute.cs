namespace Telepath.Routing;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class CallbackPrefixAttribute(string prefix) : Attribute
{
    public string Prefix { get; } = prefix;
}
