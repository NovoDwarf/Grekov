using System.Runtime.CompilerServices;
using System.Reflection;
using Grekov.Abstractions;
using Grekov.Abstractions.Attributes;
using Grekov.Abstractions.Interfaces.Definitions;

namespace Grekov.Definitions.Materializations;

internal sealed class DefFieldApplier
{
    private static readonly ConditionalWeakTable<Type, IReadOnlyDictionary<string, DefFieldMetadata>> PropertyCache = [];

    private readonly IDefConverter _converter;

    public DefFieldApplier(IDefConverter converter)
    {
        _converter = converter;
    }

    public void Apply(Def definition, IReadOnlyDictionary<string, DefValue> fields)
    {
        Apply(definition, fields, _converter);
    }

    internal static void Apply(object instance, IReadOnlyDictionary<string, DefValue> fields, IDefConverter converter)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(converter);

        var properties = GetProperties(instance.GetType());
        var description = instance is Def definition
            ? $"definition [{definition.Id}] of type [{instance.GetType().Name}]"
            : $"object of type [{instance.GetType().Name}]";

        foreach (var (name, value) in fields)
        {
            if (!properties.TryGetValue(name, out var metadata))
            {
                throw new InvalidOperationException($"Unknown field [{name}] in {description}.");
            }

            ApplyValue(instance, metadata, name, value, converter, description);
        }

        ValidateRequiredFields(properties, fields, description);
    }

    private static void ApplyValue(
        object instance,
        DefFieldMetadata metadata,
        string fieldName,
        DefValue value,
        IDefConverter converter,
        string description)
    {
        object? converted;

        try
        {
            converted = converter.Convert(value, metadata.Property.PropertyType);
        }
        catch (Exception exception) when (exception is InvalidOperationException or FormatException or ArgumentException)
        {
            throw new InvalidOperationException(
                $"Failed to convert field [{fieldName}] of {description} to type [{metadata.Property.PropertyType.Name}].",
                exception);
        }

        try
        {
            metadata.Property.SetValue(instance, converted);
        }
        catch (Exception exception) when (exception is TargetException or ArgumentException or TargetInvocationException)
        {
            throw new InvalidOperationException($"Failed to assign field [{fieldName}] of {description}.", exception);
        }
    }

    private static void ValidateRequiredFields(
        IReadOnlyDictionary<string, DefFieldMetadata> properties,
        IReadOnlyDictionary<string, DefValue> fields,
        string description)
    {
        foreach (var (name, metadata) in properties)
        {
            if (!metadata.Required || fields.Keys.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            throw new InvalidOperationException($"Required field [{name}] is missing in {description}.");
        }
    }

    private static IReadOnlyDictionary<string, DefFieldMetadata> GetProperties(Type type)
    {
        return PropertyCache.GetValue(type, static definitionType => ScanProperties(definitionType));
    }

    private static IReadOnlyDictionary<string, DefFieldMetadata> ScanProperties(Type type)
    {
        var result = new Dictionary<string, DefFieldMetadata>(StringComparer.OrdinalIgnoreCase);
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

        foreach (var property in properties)
        {
            if (property.GetIndexParameters().Length != 0 ||
                property.SetMethod is not { IsPublic: true, IsStatic: false })
            {
                continue;
            }

            var attribute = property.GetCustomAttribute<DefFieldAttribute>(inherit: true);

            if (attribute is null)
            {
                continue;
            }

            var fieldName = string.IsNullOrWhiteSpace(attribute.Name)
                ? property.Name
                : attribute.Name;

            if (result.TryAdd(fieldName, new DefFieldMetadata(property, attribute.Required)))
            {
                continue;
            }

            var existing = result[fieldName];
            throw new InvalidOperationException(
                $"Duplicate definition field [{fieldName}] in type [{type.Name}]: [{existing.Property.Name}] and [{property.Name}].");
        }

        return result;
    }

    private sealed record DefFieldMetadata(PropertyInfo Property, bool Required);
}
