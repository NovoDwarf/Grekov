using Grekov.Abstractions;
using Grekov.Abstractions.Interfaces.Definitions;

namespace Grekov.Tests.Shared.Definitions;

public sealed class PayloadConverter : IDefValueConverter
{
    public bool CanConvert(DefValue value, Type targetType) => targetType == typeof(Payload);
    
    public object Convert(DefValue value, Type targetType, IDefConverter converter) => new Payload(99);
}
