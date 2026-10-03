using Grekov.Abstractions;
using Grekov.Abstractions.Interfaces.Definitions;
using Grekov.Definitions.Registry;

namespace Grekov.Definitions.Conversions;

public sealed class DefValueConverter : IDefConverter
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

        throw new NotSupportedException($"Cannot convert [{value.Kind}] to [{targetType}].");
    }
}
