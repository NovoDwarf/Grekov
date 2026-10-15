namespace Grekov.Abstractions.Attributes;

/// <summary>
/// Requires a <see cref="DefRef"/> to resolve to its declared definition type.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DefReferenceAttribute : DefValidationAttribute
{
	/// <summary>Allows null or an empty reference. Nonempty references must still resolve.</summary>
	public bool AllowEmpty { get; set; }
}