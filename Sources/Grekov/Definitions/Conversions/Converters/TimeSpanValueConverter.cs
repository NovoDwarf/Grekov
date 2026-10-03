using System.Globalization;
using Grekov.Abstractions;
using Grekov.Abstractions.Interfaces.Definitions;

namespace Grekov.Definitions.Conversions.Converters;

internal sealed class TimeSpanValueConverter : IDefValueConverter
{
	public bool CanConvert(DefValue value, Type targetType) 
		=> DefValueConversion.IsScalar(value) && DefValueConversion.UnwrapNullable(targetType) == typeof(TimeSpan);

	public object? Convert(DefValue value, Type targetType, IDefConverter converter)
	{
		var text = DefValueConversion.ToInvariantString(value.Scalar!);

		return TimeSpan.Parse(text, CultureInfo.InvariantCulture);
	}
}