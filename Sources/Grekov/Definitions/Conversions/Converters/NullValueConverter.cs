using Grekov.Abstractions;
using Grekov.Abstractions.Enums;
using Grekov.Abstractions.Interfaces.Definitions;

namespace Grekov.Definitions.Conversions.Converters;

internal sealed class NullValueConverter : IDefValueConverter
{
	public bool CanConvert(DefValue value, Type targetType) 
		=> value.Kind == DefValueKind.Null;

	public object? Convert(DefValue value, Type targetType, IDefConverter converter)
	{
		if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) is null)
			throw new InvalidOperationException($"Cannot assign null to '{targetType.FullName}'.");

		return null;
	}
}