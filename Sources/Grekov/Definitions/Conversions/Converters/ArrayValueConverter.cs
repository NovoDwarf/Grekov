using Grekov.Abstractions;
using Grekov.Abstractions.Enums;
using Grekov.Abstractions.Interfaces.Definitions;

namespace Grekov.Definitions.Conversions.Converters;

internal sealed class ArrayValueConverter : IDefValueConverter
{
	public bool CanConvert(DefValue value, Type targetType)
	{
		return targetType.IsArray && targetType.GetArrayRank() == 1;
	}

	public object Convert(DefValue value, Type targetType, IDefConverter converter)
	{
		var values = GetValues(value, targetType);
		var elementType = targetType.GetElementType()!;

		var array = Array.CreateInstance(elementType, values.Count);

		for (var i = 0; i < values.Count; i++)
		{
			var item = converter.Convert(values[i], elementType);
			array.SetValue(item, i);
		}

		return array;
	}

	private static IReadOnlyList<DefValue> GetValues(DefValue value, Type targetType)
	{
		if (value.Kind == DefValueKind.List)
			return value.List!;

		if (value is { Kind: DefValueKind.Object, Object: { Count: 1 } objectValue } && objectValue.Values.First() is { Kind: DefValueKind.List, List: not null } listValue)
			return listValue.List!;

		throw new InvalidOperationException($"Expected list value for [{targetType}], got [{value.Kind}].");
	}
}