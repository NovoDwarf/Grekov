using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Grekov.Abstractions;
using Grekov.Abstractions.Attributes;
using Grekov.Abstractions.Interfaces.Definitions;

namespace Grekov.Definitions.Validation;

internal sealed class DefReferenceValidator : IDefAttributeValidator<DefReferenceAttribute>
{
	private readonly ConcurrentDictionary<Type, Func<object, DefId>> _readers = new();

	public IEnumerable<ValidationResult> Validate(
		DefReferenceAttribute attribute,
		object? value,
		DefValidationContext context)
	{
		var referenceType = Nullable.GetUnderlyingType(context.ValueType) ?? context.ValueType;

		if (!referenceType.IsGenericType || referenceType.GetGenericTypeDefinition() != typeof(DefRef<>))
		{
			throw new InvalidOperationException(
				$"[DefReference] requires a DefRef<T> property. Member: [{context.MemberPath}], type: [{context.ValueType}].");
		}

		var id = value is null ? default : _readers.GetOrAdd(referenceType, CreateReader)(value);

		if (id.IsEmpty)
		{
			if (!attribute.AllowEmpty)
			{
				yield return new ValidationResult("A nonempty definition reference is required.");
			}

			yield break;
		}

		var definitionType = referenceType.GenericTypeArguments[0];

		if (!context.Catalog.Contains(definitionType, id))
		{
			yield return new ValidationResult($"Reference [{id}] must resolve to [{definitionType.Name}].");
		}
	}

	private static Func<object, DefId> CreateReader(Type referenceType)
	{
		return (Func<object, DefId>)typeof(DefReferenceValidator)
		                            .GetMethod(nameof(ReadId), BindingFlags.NonPublic | BindingFlags.Static)!
		                            .MakeGenericMethod(referenceType.GenericTypeArguments[0])
		                            .CreateDelegate(typeof(Func<object, DefId>));
	}

	private static DefId ReadId<T>(object value) where T : Def
	{
		return ((DefRef<T>)value).Id;
	}
}