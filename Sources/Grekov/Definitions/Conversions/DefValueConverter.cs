using System.Collections;
using Grekov.Core;
using Grekov.Definitions.Registry;

namespace Grekov.Definitions.Conversions;

internal sealed class DefValueConverter
{
    private readonly DefValueConverterRegistry _registry;

    public DefValueConverter(DefValueConverterRegistry registry)
    {
        _registry = registry;
    }

    public object? Convert(DefValue value, Type targetType)
    {
        if (targetType.IsGenericType)
        {
            var genericType = targetType.GetGenericTypeDefinition();

            if (genericType == typeof(Dictionary<,>))
                return ConvertDictionary(value, targetType);

            if (genericType == typeof(List<>))
                return ConvertList(value, targetType);
        }

        foreach (var converter in _registry.Converters)
        {
            if (converter.CanConvert(value, targetType))
            {
                return converter.Convert(value, targetType);
            }
        }

        throw new NotSupportedException(
            $"Cannot convert [{value.Kind}] to [{targetType}].");
    }

    private object ConvertDictionary(DefValue value, Type targetType)
    {
        var entries = value.Object ?? throw new InvalidOperationException("Expected object value.");

        var arguments = targetType.GetGenericArguments();

        var keyType = arguments[0];
        var valueType = arguments[1];

        var dictionaryType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
        var dictionary = (IDictionary)Activator.CreateInstance(dictionaryType)!;

        foreach (var entry in entries)
        {
            var key = Convert(DefValue.ScalarValue(entry.Key), keyType);
            var item = Convert(entry.Value, valueType);

            dictionary.Add(key!, item);
        }

        return dictionary;
    }

    private object ConvertList(DefValue value, Type targetType)
    {
        var values = value.List ?? throw new InvalidOperationException("Expected list value.");

        var itemType = targetType.GetGenericArguments()[0];
        var listType = typeof(List<>).MakeGenericType(itemType);
        var list = (IList)Activator.CreateInstance(listType)!;

        foreach (var item in values)
        {
            list.Add(Convert(item, itemType));
        }

        return list;
    }
}