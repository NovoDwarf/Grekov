using Grekov.Core;
using Grekov.Definitions.Interfaces;

namespace Grekov.Definitions.Conversions.Converters;

internal sealed class BooleanValueConverter : IDefValueConverter
{
	public bool CanConvert(DefValue value, Type targetType)
	{
		return DefValueConversion.IsScalar(value) && DefValueConversion.UnwrapNullable(targetType) == typeof(bool);
	}

	public object Convert(DefValue value, Type targetType)
	{
		var scalar = value.Scalar!;

		switch (scalar)
		{
			case bool boolean: return boolean;
			case string text when bool.TryParse(text, out var parsed): return parsed;
			case string text: throw new FormatException($"Value '{text}' is not a valid boolean.");
			case int or long or short or byte: return System.Convert.ToInt32(scalar) != 0;
			default: throw new FormatException($"Cannot convert '{scalar.GetType().FullName}' to bool.");
		}
	}
}