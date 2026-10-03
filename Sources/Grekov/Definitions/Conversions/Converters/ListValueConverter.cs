using System.Collections;
using Grekov.Core;
using Grekov.Definitions.Interfaces;

namespace Grekov.Definitions.Conversions.Converters;

internal sealed class ListValueConverter : IDefValueConverter
{
	public bool CanConvert(DefValue value, Type targetType)
	{
		return targetType.IsGenericType &&
		       targetType.GetGenericTypeDefinition() == typeof(List<>);
	}

	public object Convert(DefValue value, Type targetType, DefValueConverter converter)
	{
		var values = DefValueConversion.GetListValues(value, targetType);
		var itemType = targetType.GetGenericArguments()[0];
		var listType = typeof(List<>).MakeGenericType(itemType);
		var list = (IList)Activator.CreateInstance(listType)!;

		foreach (var item in values) 
			list.Add(converter.Convert(item, itemType)!);

		return list;
	}
}