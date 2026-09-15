using System.Globalization;
using Grekov.Core;
using Grekov.Core.Enums;

namespace Grekov.Definitions.Conversions;

internal static class DefValueConversion
{
	public static Type UnwrapNullable(Type type)
	{
		return Nullable.GetUnderlyingType(type) ?? type;
	}

	public static string ToInvariantString(object value)
	{
		return Convert.ToString(value, CultureInfo.InvariantCulture)!;
	}

	public static bool IsScalar(DefValue value)
	{
		return value is { Kind: DefValueKind.Scalar, Scalar: not null };
	}
}