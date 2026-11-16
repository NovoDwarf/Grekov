using System.ComponentModel.DataAnnotations;
using Grekov.Abstractions.Attributes;

namespace Grekov.Abstractions.Interfaces.Definitions;

public interface IDefAttributeValidator<in TAttribute> where TAttribute : DefValidationAttribute
{
	/// <summary>Returns errors relative to the annotated property or object.</summary>
	IEnumerable<ValidationResult> Validate(TAttribute attribute, object? value, DefValidationContext context);
}