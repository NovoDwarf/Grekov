using Grekov.Abstractions;
using Grekov.Abstractions.Enums;
using Grekov.Abstractions.Interfaces.Definitions;
using Grekov.Definitions.Materializations;

namespace Grekov.Definitions.Conversions.Converters;

internal sealed class ObjectValueConverter : IDefValueConverter
{
    public bool CanConvert(DefValue value, Type targetType)
    {
        return value.Kind == DefValueKind.Object;
    }

    public object Convert(DefValue value, Type targetType, IDefConverter converter)
    {
        var entries = value.Object ?? throw new InvalidOperationException("Expected object value.");
        var instance = Activator.CreateInstance(targetType)
            ?? throw new InvalidOperationException($"Cannot create instance of [{targetType}].");

        DefFieldApplier.Apply(instance, entries, converter);
        return instance;
    }
}
