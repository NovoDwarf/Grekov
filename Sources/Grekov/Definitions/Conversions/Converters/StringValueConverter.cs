using Grekov.Abstractions;
using Grekov.Abstractions.Interfaces.Definitions;

namespace Grekov.Definitions.Conversions.Converters;

internal sealed class StringValueConverter : IDefValueConverter
{
	public bool CanConvert(DefValue value, Type targetType) 
		=> DefValueConversion.IsScalar(value) && DefValueConversion.UnwrapNullable(targetType) == typeof(string);

	public object? Convert(DefValue value, Type targetType, IDefConverter converter)
		=> DefValueConversion.ToInvariantString(value.Scalar!);
}