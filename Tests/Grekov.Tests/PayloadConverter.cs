using Grekov.Abstractions;
using Grekov.Abstractions.Interfaces.Definitions;
using Grekov.Definitions.Conversions;
using Grekov.Definitions.Registry;
using Microsoft.Extensions.DependencyInjection;

namespace Grekov.Tests;

public sealed class PayloadConverter : IDefValueConverter
{
    public bool CanConvert(DefValue value, Type targetType) => targetType == typeof(Payload);
    public object Convert(DefValue value, Type targetType, IDefConverter converter) => new Payload(99);
}
