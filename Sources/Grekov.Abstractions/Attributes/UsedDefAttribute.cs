namespace Grekov.Abstractions.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class UsedDefAttribute<T> : Attribute
{
	public Type Type => typeof(T);
}