using Grekov.Core;
using Grekov.Definitions.Interfaces;

namespace Grekov.Definitions.Conversions.Converters;

internal sealed class DefRefValueConverter : IDefValueConverter
{
	public bool CanConvert(DefValue value, Type targetType)
	{
		var actualType = DefValueConversion.UnwrapNullable(targetType);

		return DefValueConversion.IsScalar(value) && IsDefRef(actualType);
	}

	public object Convert(DefValue value, Type targetType)
	{
		var actualType = DefValueConversion.UnwrapNullable(targetType);

		var text = DefValueConversion.ToInvariantString(value.Scalar!);
		var id = DefId.Parse(text);

		return Activator.CreateInstance(actualType, id) ?? throw new InvalidOperationException( $"Could not create '{actualType.FullName}'.");
	}

	private static bool IsDefRef(Type type) 
		=> type.IsGenericType && type.GetGenericTypeDefinition() == typeof(DefRef<>);
}