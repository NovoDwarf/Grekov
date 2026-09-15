namespace Grekov.Core.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class DefTypeAttribute : Attribute
{
	public DefTypeAttribute(string? elementName = null)
	{
		ElementName = elementName;
	}

	public string? ElementName { get; }
}
