using System.Collections;
using Grekov.Core;
using Grekov.Definitions.Interfaces;

namespace Grekov.Definitions.Conversions.Converters;

internal sealed class DictionaryValueConverter : IDefValueConverter
{
	public bool CanConvert(DefValue value, Type targetType)
	{
		return targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Dictionary<,>);
	}

	public object Convert(DefValue value, Type targetType, DefValueConverter converter)
	{
		var entries = value.Object ?? throw new InvalidOperationException($"Expected object value for dictionary [{targetType}].");

		var arguments = targetType.GetGenericArguments();
		var keyType = arguments[0];
		var valueType = arguments[1];

		var dictionaryType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
		var dictionary = (IDictionary)Activator.CreateInstance(dictionaryType)!;

		foreach (var entry in entries)
		{
			var key = converter.Convert(DefValue.ScalarValue(entry.Key), keyType);
			var item = converter.Convert(entry.Value, valueType);

			dictionary.Add(key!, item);
		}

		return dictionary;
	}
}