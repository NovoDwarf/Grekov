using Grekov.Core;
using Grekov.Core.Enums;
using Grekov.Definitions.Interfaces;

namespace Grekov.Definitions.Conversions.Converters;

internal sealed class NullValueConverter : IDefValueConverter
{
	public bool CanConvert(DefValue value, Type targetType) 
		=> value.Kind == DefValueKind.Null;

	public object? Convert(DefValue value, Type targetType)
	{
		ArgumentNullException.ThrowIfNull(targetType);

		if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) is null)
			throw new InvalidOperationException($"Cannot assign null to '{targetType.FullName}'.");

		return null;
	}
}