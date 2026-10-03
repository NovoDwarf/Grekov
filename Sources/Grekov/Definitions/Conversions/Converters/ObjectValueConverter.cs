using System.Reflection;
using Grekov.Core;
using Grekov.Core.Attributes;
using Grekov.Core.Enums;
using Grekov.Definitions.Interfaces;

namespace Grekov.Definitions.Conversions.Converters;

internal sealed class ObjectValueConverter : IDefValueConverter
{
	public bool CanConvert(DefValue value, Type targetType)
	{
		return value.Kind == DefValueKind.Object;
	}

	public object Convert(DefValue value, Type targetType, DefValueConverter converter)
	{
		var entries = value.Object ?? throw new InvalidOperationException("Expected object value.");
		var instance = Activator.CreateInstance(targetType) ?? throw new InvalidOperationException($"Cannot create instance of [{targetType}].");
		var fields = targetType
		             .GetProperties(BindingFlags.Instance | BindingFlags.Public)
		             .Select(static property => new { Property = property, Attribute = property.GetCustomAttribute<DefFieldAttribute>() })
		             .Where(static x => x.Attribute is not null)
		             .ToDictionary(static x => x.Attribute!.Name ?? x.Property.Name, StringComparer.OrdinalIgnoreCase);

		foreach (var entry in entries)
		{
			if (!fields.TryGetValue(entry.Key, out var field))
				throw new InvalidOperationException($"Unknown field [{entry.Key}] for type [{targetType}].");

			var converted = converter.Convert(entry.Value, field.Property.PropertyType);
			field.Property.SetValue(instance, converted);
		}

		return instance;
	}
}