using Grekov.Core;
using Grekov.Definitions.Interfaces;
using Grekov.Definitions.Models;
using Grekov.Definitions.Registry;
using Grekov.Packaging.Entities;
using NovoDwarf.FS.Files.Interfaces;
using NovoDwarf.FS.Paths.Interfaces;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Grekov.Readers.Yaml;

internal sealed class DefYamlReader : IDefFormatReader
{
    private static readonly IReadOnlySet<string> SupportedExtensions = new HashSet<string>([".yaml", ".yml"], StringComparer.OrdinalIgnoreCase);
    private static readonly IReadOnlySet<string> ReservedProperties = new HashSet<string>(["$type", "Id", "Parent"], StringComparer.OrdinalIgnoreCase);

    private readonly DefTypeRegistry _typeRegistry;
    private readonly IFileStreamer _fileStreamer;
    private readonly IPathParser _pathParser;

    private readonly IDeserializer _deserializer;

    public DefYamlReader(DefTypeRegistry typeRegistry, IPathParser pathParser, IFileStreamer fileStreamer)
    {
        _typeRegistry = typeRegistry;
        _pathParser = pathParser;
        _fileStreamer = fileStreamer;

        _deserializer = new DeserializerBuilder().WithNamingConvention(NullNamingConvention.Instance).Build();
    }

    public IReadOnlySet<string> Extensions => SupportedExtensions;

    public PackageManifest ReadManifest(string manifestPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestPath);

        try
        {
            using var stream = _fileStreamer.OpenRead(manifestPath);
            using var reader = new StreamReader(stream);

            var manifest = _deserializer.Deserialize<PackageManifest>(reader);

            if (manifest is null)
                throw new InvalidDataException("YAML manifest does not contain a valid package manifest.");

            if (string.IsNullOrWhiteSpace(manifest.Id))
                manifest.Id = _pathParser.GetDirectoryName(manifestPath);

            return manifest;
        }
        catch (Exception exception) when (exception is YamlException or InvalidDataException)
        {
            throw new InvalidDataException($"Invalid YAML package manifest: [{manifestPath}].", exception);
        }
    }

    public IReadOnlyList<DefRaw> ReadDefinitions(DefReadContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        using var stream = _fileStreamer.OpenRead(context.ResourcePath);
        using var reader = new StreamReader(stream);

        try
        {
            var document = _deserializer.Deserialize<Dictionary<string, object?>>(reader);

            if (document is null)
                throw Error(context, "YAML definition file is empty.");

            return [ReadDefinition(document, context)];
        }
        catch (YamlException exception)
        {
            throw Error(context, $"Invalid YAML definition file: [{context.ResourcePath}].", exception);
        }
    }

    private DefRaw ReadDefinition(Dictionary<string, object?> document, DefReadContext context)
    {
        var typeName = ReadRequiredString(document, "$type", context);

        if (!_typeRegistry.TryResolveByElementName(typeName, out var type))
            throw Error(context, $"Unknown definition type [{typeName}].");

        var id = ReadRequiredId(document, context);
        var parentId = ReadOptionalParentId(document, context);

        var fields = ReadFields(document, context);

        fields.Remove("$type");
        fields.Remove("Id");
        fields.Remove("Parent");

        return new DefRaw
        {
            Id = id,
            Type = type,
            ParentId = parentId,
            Fields = fields,
            PackageId = context.PackageId,
            ResourcePath = context.ResourcePath
        };
    }

    private static DefId ReadRequiredId(Dictionary<string, object?> document, DefReadContext context)
    {
        var value = ReadRequiredString(document, "Id", context);

        try
        {
            return DefId.Parse(value);
        }
        catch (Exception exception)when (exception is ArgumentException or FormatException)
        {
            throw Error(context, $"Invalid definition Id [{value}].", exception);
        }
    }

    private static DefId? ReadOptionalParentId(
        Dictionary<string, object?> document,
        DefReadContext context)
    {
        if (!TryGetValue(document, "Parent", out var rawValue))
            return null;

        if (rawValue is not string value)
            throw Error(context, "Definition [Parent] must be a string.");

        if (string.IsNullOrWhiteSpace(value))
            return null;

        try
        {
            return DefId.Parse(value);
        }
        catch (Exception exception)when (exception is ArgumentException or FormatException)
        {
            throw Error(context, $"Invalid parent Id [{value}].", exception);
        }
    }

    private static Dictionary<string, DefValue> ReadFields(Dictionary<string, object?> document, DefReadContext context)
    {
        var fields = new Dictionary<string, DefValue>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in document)
        {
            if (ReservedProperties.Contains(pair.Key))
                continue;

            AddField(fields, pair.Key, ReadValue(pair.Value, context), context);
        }

        return fields;
    }

    private static DefValue ReadValue(
        object? value,
        DefReadContext context)
    {
        if (value is null)
            return DefValue.Null();

        return value switch
        {
            IDictionary<object, object?> map => ReadObject(map, context),
            IDictionary<string, object?> map => ReadObject(map, context),
            IEnumerable<object?> sequence => ReadArray(sequence, context),
            string stringValue => DefValue.ScalarValue(stringValue),
            bool boolValue => DefValue.ScalarValue(boolValue),
            byte byteValue => DefValue.ScalarValue(byteValue),
            sbyte sbyteValue => DefValue.ScalarValue(sbyteValue),
            short shortValue => DefValue.ScalarValue(shortValue),
            ushort ushortValue => DefValue.ScalarValue(ushortValue),
            int intValue => DefValue.ScalarValue(intValue),
            uint uintValue => DefValue.ScalarValue(uintValue),
            long longValue => DefValue.ScalarValue(longValue),
            ulong ulongValue => DefValue.ScalarValue(ulongValue),
            float floatValue => DefValue.ScalarValue(floatValue),
            double doubleValue => DefValue.ScalarValue(doubleValue),
            decimal decimalValue => DefValue.ScalarValue(decimalValue),
            DateTime dateTime => DefValue.ScalarValue(dateTime),
            DateTimeOffset dateTimeOffset => DefValue.ScalarValue(dateTimeOffset),
            DateOnly dateOnly => DefValue.ScalarValue(dateOnly),
            TimeOnly timeOnly => DefValue.ScalarValue(timeOnly),
            _ => throw Error(context, $"Unsupported YAML value type [{value.GetType().FullName}].")
        };
    }

    private static DefValue ReadObject(IDictionary<object, object?> map, DefReadContext context)
    {
        var fields = new Dictionary<string, DefValue>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in map)
        {
            if (pair.Key is not string name)
                throw Error(context, "YAML object property names must be strings.");

            AddField(fields, name, ReadValue(pair.Value, context), context);
        }

        return DefValue.ObjectValue(fields);
    }

    private static DefValue ReadObject(IDictionary<string, object?> map, DefReadContext context)
    {
        var fields = new Dictionary<string, DefValue>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in map) 
            AddField(fields, pair.Key, ReadValue(pair.Value, context), context);

        return DefValue.ObjectValue(fields);
    }

    private static DefValue ReadArray(IEnumerable<object?> sequence, DefReadContext context)
    {
        var values = sequence
            .Select(value => ReadValue(value, context))
            .ToArray();

        return DefValue.ListValue(values);
    }

    private static string ReadRequiredString(Dictionary<string, object?> document, string name, DefReadContext context)
    {
        if (!TryGetValue(document, name, out var value))
            throw Error(context, $"Definition does not contain required [{name}].");

        if (value is not string stringValue)
            throw Error(context, $"Definition [{name}] must be a string.");

        if (string.IsNullOrWhiteSpace(stringValue))
            throw Error(context, $"Definition [{name}] cannot be empty.");

        return stringValue;
    }

    private static bool TryGetValue(Dictionary<string, object?> document, string name, out object? value)
    {
        foreach (var pair in document)
        {
            if (!pair.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
                continue;

            value = pair.Value;
            return true;
        }

        value = null;
        return false;
    }

    private static void AddField(Dictionary<string, DefValue> fields, string name, DefValue value, DefReadContext context)
    {
        if (!fields.TryAdd(name, value))
            throw Error(context, $"Field [{name}] is declared more than once.");
    }

    private static InvalidDataException Error(DefReadContext context, string message, Exception? innerException = null)
    {
        return new InvalidDataException($"{message} File: [{context.ResourcePath}]", innerException);
    }
}