namespace Grekov.Abstractions.Attributes;

/// <summary>Declares a rule executed by registered definition attribute validators.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
public abstract class DefValidationAttribute : Attribute
{
	
}