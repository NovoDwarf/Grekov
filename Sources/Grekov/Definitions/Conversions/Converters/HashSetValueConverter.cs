using System.Collections;
using Grekov.Abstractions;
using Grekov.Abstractions.Enums;
using Grekov.Abstractions.Interfaces.Definitions;

namespace Grekov.Definitions.Conversions.Converters;

internal sealed class HashSetValueConverter : IDefValueConverter
{
    public bool CanConvert(DefValue value, Type targetType)
    {
        return targetType.IsGenericType &&
               targetType.GetGenericTypeDefinition() == typeof(HashSet<>);
    }

    public object Convert(DefValue value, Type targetType, IDefConverter converter)
    {
        var values = GetValues(value, targetType);
        var elementType = targetType.GetGenericArguments()[0];

        var setType = typeof(HashSet<>).MakeGenericType(elementType);
        var set = Activator.CreateInstance(setType)!;
        var add = setType.GetMethod("Add", [elementType])!;

        foreach (var item in values)
        {
            var converted = converter.Convert(item, elementType);

            add.Invoke(set, [converted]);
        }

        return set;
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
