using Grekov.Core.Enums;

namespace Grekov.Core.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public sealed class DefFieldAttribute : Attribute
{
	public DefFieldAttribute(string? name = null)
	{
		Name = name;
	}

	public string? Name { get; }
	
	public bool Required { get; init; }
	
	public string? ItemName { get; init; }
}
