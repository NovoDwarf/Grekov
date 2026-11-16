namespace Grekov.Abstractions.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public sealed class DefFieldAttribute : Attribute
{
	public DefFieldAttribute(string? name = null)
	{
		Name = name;
	}

	public string? Name { get; }
	
	public bool Required { get; init; }
}
