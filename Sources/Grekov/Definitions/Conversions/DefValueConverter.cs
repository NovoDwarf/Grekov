using System.Collections;
using System.Reflection;
using Grekov.Core;
using Grekov.Core.Attributes;
using Grekov.Core.Enums;
using Grekov.Definitions.Registry;

namespace Grekov.Definitions.Conversions;

public sealed class DefValueConverter
{
    private readonly DefValueConverterRegistry _registry;

    public DefValueConverter(DefValueConverterRegistry registry)
    {
        _registry = registry;
    }

    public object? Convert(DefValue value, Type targetType)
    {
        foreach (var converter in _registry.Converters)
        {
            if (converter.CanConvert(value, targetType))
                return converter.Convert(value, targetType, this);
        }

        throw new NotSupportedException(
            $"Cannot convert [{value.Kind}] to [{targetType}].");
    }
}