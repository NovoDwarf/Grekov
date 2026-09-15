using Grekov.Core;
using Grekov.Definitions.Interfaces;

namespace Grekov.Definitions.Conversions.Converters;

internal sealed class DefIdValueConverter : IDefValueConverter
{
	public bool CanConvert(DefValue value, Type targetType) 
		=> DefValueConversion.IsScalar(value) && DefValueConversion.UnwrapNullable(targetType) == typeof(DefId);

	public object Convert(DefValue value, Type targetType)
	{
		var text = DefValueConversion.ToInvariantString(value.Scalar!);

		return DefId.Parse(text);
	}
}