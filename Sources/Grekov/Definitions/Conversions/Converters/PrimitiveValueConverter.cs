using System.Globalization;
using Grekov.Abstractions;
using Grekov.Abstractions.Interfaces.Definitions;

namespace Grekov.Definitions.Conversions.Converters;

internal sealed class PrimitiveValueConverter : IDefValueConverter
{
	public bool CanConvert(DefValue value, Type targetType)
	{
		if (!DefValueConversion.IsScalar(value))
			return false;

		var actualType = DefValueConversion.UnwrapNullable(targetType);

		return actualType.IsPrimitive || actualType == typeof(decimal);
	}

	public object? Convert(DefValue value, Type targetType, IDefConverter converter)
	{
		var actualType = DefValueConversion.UnwrapNullable(targetType);

		return System.Convert.ChangeType(value.Scalar!, actualType, CultureInfo.InvariantCulture)!;
	}
}