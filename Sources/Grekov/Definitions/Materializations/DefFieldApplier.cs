using System.Collections.Concurrent;
using System.Reflection;
using Grekov.Abstractions;
using Grekov.Abstractions.Attributes;
using Grekov.Abstractions.Interfaces.Definitions;

namespace Grekov.Definitions.Materializations;

internal sealed class DefFieldApplier
{
    private static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, DefFieldMetadata>> PropertyCache = [];

    private readonly IDefConverter _converter;

    public DefFieldApplier(IDefConverter converter)
    {
        _converter = converter;
    }

    public void Apply(Def definition, IReadOnlyDictionary<string, DefValue> fields)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(fields);

        var properties = GetProperties(definition.GetType());

        foreach (var (name, value) in fields)
        {
            if (!properties.TryGetValue(name, out var metadata))
                throw new InvalidOperationException($"Unknown field [{name}] in definition [{definition.Id}] of type [{definition.GetType().Name}]");

            ApplyValue(definition, metadata, name, value);
        }

        ValidateRequiredFields(definition, properties, fields);
    }

    private void ApplyValue(Def definition, DefFieldMetadata metadata, string fieldName, DefValue value)
    {
        object? converted;

        try
        {
            converted = _converter.Convert(value, metadata.Property.PropertyType);
        }
        catch (Exception exception)when (exception is InvalidOperationException or FormatException or ArgumentException)
        {
            throw new InvalidOperationException($"Failed to convert field [{fieldName}] of definition [{definition.Id}] to type [{metadata.Property.PropertyType.Name}]", exception);
        }

        try
        {
            metadata.Property.SetValue(definition, converted);
        }
        catch (Exception exception)when (exception is TargetException or ArgumentException or TargetInvocationException)
        {
            throw new InvalidOperationException($"Failed to assign field [{fieldName}] of definition [{definition.Id}]", exception);
        }
    }

    private static void ValidateRequiredFields(
        Def definition,
        IReadOnlyDictionary<string, DefFieldMetadata> properties,
        IReadOnlyDictionary<string, DefValue> fields)
    {
        foreach (var (name, metadata) in properties)
        {
            if (!metadata.Required)
                continue;

            if (fields.ContainsKey(name))
                continue;

            throw new InvalidOperationException($"Required field [{name}] is missing in definition [{definition.Id}] of type [{definition.GetType().Name}].");
        }
    }

    private static IReadOnlyDictionary<string, DefFieldMetadata> GetProperties(Type type) 
        => PropertyCache.GetOrAdd(type, static definitionType => ScanProperties(definitionType));

    private static IReadOnlyDictionary<string, DefFieldMetadata> ScanProperties(Type type)
    {
        var result = new Dictionary<string, DefFieldMetadata>(StringComparer.OrdinalIgnoreCase);
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

        foreach (var property in properties)
        {
            if (property.GetIndexParameters().Length != 0)
                continue;

            if (property.SetMethod is not { IsPublic: true })
                continue;

            if (property.SetMethod.IsStatic)
                continue;

            var attribute = property.GetCustomAttribute<DefFieldAttribute>(
                inherit: true);

            if (attribute is null)
                continue;

            var fieldName = attribute.Name;

            if (string.IsNullOrWhiteSpace(fieldName))
                fieldName = property.Name;

            if (result.TryAdd(fieldName, new DefFieldMetadata(property, attribute.Required))) 
                continue;
            
            var existing = result[fieldName];

            throw new InvalidOperationException($"Duplicate definition field [{fieldName}] in type [{type.Name}]: [{existing.Property.Name}] and [{property.Name}].");
        }

        return result;
    }

    private sealed record DefFieldMetadata(PropertyInfo Property, bool Required);
}