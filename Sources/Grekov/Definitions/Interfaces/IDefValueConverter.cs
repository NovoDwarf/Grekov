using Grekov.Core;
using Grekov.Definitions.Conversions;

namespace Grekov.Definitions.Interfaces;

public interface IDefValueConverter
{
	public bool CanConvert(DefValue value, Type targetType);

	public object? Convert(DefValue value, Type targetType, DefValueConverter converter);
}