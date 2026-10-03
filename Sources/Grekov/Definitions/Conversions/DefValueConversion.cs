using System.Globalization;
using Grekov.Core;
using Grekov.Core.Enums;

namespace Grekov.Definitions.Conversions;

internal static class DefValueConversion
{
	public static Type UnwrapNullable(Type type) => Nullable.GetUnderlyingType(type) ?? type;

	public static string ToInvariantString(object value) => Convert.ToString(value, CultureInfo.InvariantCulture)!;

	public static bool IsScalar(DefValue value) => value is { Kind: DefValueKind.Scalar, Scalar: not null };

	public static IReadOnlyList<DefValue> GetListValues(
		DefValue value,
		Type targetType)
	{
		if (value.Kind == DefValueKind.List)
			return value.List!;

		if (value is not { Kind: DefValueKind.Object, Object: { Count: 1 } objectValue }) 
			return [value];
		
		var child = objectValue.Values.First();

		if (child.Kind == DefValueKind.List)
			return child.List!;

		return [child];

	}
}