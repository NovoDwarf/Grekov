using Grekov.Core;
using Grekov.Definitions.Interfaces;

namespace Grekov.Definitions.Conversions.Converters;

internal sealed class EnumValueConverter : IDefValueConverter
{
	public bool CanConvert(DefValue value, Type targetType)
	{
		var actualType = DefValueConversion.UnwrapNullable(targetType);

		return DefValueConversion.IsScalar(value) && actualType.IsEnum;
	}

	public object Convert(DefValue value, Type targetType)
	{
		var enumType = DefValueConversion.UnwrapNullable(targetType);
		var scalar = value.Scalar!;

		if (scalar is string name)
			return Enum.Parse(enumType, name, ignoreCase: true);

		return Enum.ToObject(enumType, scalar);
	}
}