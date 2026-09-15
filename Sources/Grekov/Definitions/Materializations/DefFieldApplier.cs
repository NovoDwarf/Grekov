using System.Collections.Concurrent;
using System.Reflection;
using Grekov.Core;
using Grekov.Core.Attributes;
using Grekov.Definitions.Conversions;

namespace Grekov.Definitions.Materializations;

internal sealed class DefFieldApplier
{
    private static readonly ConcurrentDictionary<
        Type,
        IReadOnlyDictionary<string, PropertyInfo>> PropertyCache = [];

    private readonly DefValueConverter _converter;

    public DefFieldApplier(
        DefValueConverter converter)
    {
        ArgumentNullException.ThrowIfNull(converter);

        _converter = converter;
    }

    public void Apply(
        Def definition,
        IReadOnlyDictionary<string, DefValue> fields)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(fields);

        var properties =
            GetProperties(
                definition.GetType());

        foreach (var (name, value) in fields)
        {
            if (!properties.TryGetValue(
                    name,
                    out var property))
            {
                throw new InvalidOperationException(
                    $"Unknown field '{name}' in definition " +
                    $"'{definition.Id}' of type " +
                    $"'{definition.GetType().Name}'.");
            }

            ApplyValue(
                definition,
                property,
                name,
                value);
        }
    }

    private void ApplyValue(
        Def definition,
        PropertyInfo property,
        string fieldName,
        DefValue value)
    {
        object? converted;

        try
        {
            converted =
                _converter.Convert(
                    value,
                    property.PropertyType);
        }
        catch (Exception exception)
            when (exception is
                InvalidOperationException or
                FormatException or
                ArgumentException)
        {
            throw new InvalidOperationException(
                $"Failed to convert field '{fieldName}' " +
                $"of definition '{definition.Id}' " +
                $"to type '{property.PropertyType.Name}'.",
                exception);
        }

        try
        {
            property.SetValue(
                definition,
                converted);
        }
        catch (Exception exception)
            when (exception is
                TargetException or
                ArgumentException or
                TargetInvocationException)
        {
            throw new InvalidOperationException(
                $"Failed to assign field '{fieldName}' " +
                $"of definition '{definition.Id}'.",
                exception);
        }
    }

    private static IReadOnlyDictionary<string, PropertyInfo> GetProperties(
        Type type)
    {
        return PropertyCache.GetOrAdd(
            type,
            static definitionType =>
                ScanProperties(definitionType));
    }

    private static IReadOnlyDictionary<string, PropertyInfo> ScanProperties(
        Type type)
    {
        var result =
            new Dictionary<string, PropertyInfo>(
                StringComparer.OrdinalIgnoreCase);

        var properties =
            type.GetProperties(
                BindingFlags.Instance |
                BindingFlags.Public);

        foreach (var property in properties)
        {
            if (property.GetIndexParameters().Length != 0)
                continue;

            if (property.GetMethod is null ||
                property.SetMethod is null)
                continue;

            if (property.GetMethod.IsStatic ||
                property.SetMethod.IsStatic)
                continue;

            var attribute =
                property.GetCustomAttribute<DefFieldAttribute>(
                    inherit: true);

            if (attribute is null)
                continue;

            var fieldName =
                attribute.Name ?? property.Name;

            if (string.IsNullOrWhiteSpace(fieldName))
            {
                throw new InvalidOperationException(
                    $"Definition property '{type.Name}.{property.Name}' " +
                    "has an empty field name.");
            }

            if (!result.TryAdd(
                    fieldName,
                    property))
            {
                var existing =
                    result[fieldName];

                throw new InvalidOperationException(
                    $"Duplicate definition field '{fieldName}' " +
                    $"in type '{type.Name}': " +
                    $"'{existing.Name}' and '{property.Name}'.");
            }
        }

        return result;
    }
}