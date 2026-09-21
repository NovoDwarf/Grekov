using Tomlyn;
using Tomlyn.Model;
using Grekov.Core;
using Grekov.Definitions.Interfaces;
using Grekov.Definitions.Models;
using Grekov.Definitions.Registry;
using Grekov.Packaging.Entities;
using NovoDwarf.FS.Files.Interfaces;
using NovoDwarf.FS.Paths.Interfaces;

namespace Grekov.Readers.Toml;

internal sealed class DefTomlReader : IDefFormatReader
{
    private static readonly IReadOnlySet<string> SupportedExtensions = new HashSet<string>([".toml"], StringComparer.OrdinalIgnoreCase);
    private static readonly IReadOnlySet<string> ReservedProperties = new HashSet<string>(["$type", "Id", "Parent"], StringComparer.OrdinalIgnoreCase);

    private readonly DefTypeRegistry _typeRegistry;
    private readonly IFileStreamer _fileStreamer;
    private readonly IPathParser _pathParser;

    public DefTomlReader(
        DefTypeRegistry typeRegistry,
        IPathParser pathParser,
        IFileStreamer fileStreamer)
    {
        ArgumentNullException.ThrowIfNull(typeRegistry);
        ArgumentNullException.ThrowIfNull(pathParser);
        ArgumentNullException.ThrowIfNull(fileStreamer);

        _typeRegistry = typeRegistry;
        _pathParser = pathParser;
        _fileStreamer = fileStreamer;
    }

    public IReadOnlySet<string> Extensions => SupportedExtensions;

    public PackageManifest ReadManifest(string manifestPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestPath);

        try
        {
            using var stream = _fileStreamer.OpenRead(manifestPath);
            using var reader = new StreamReader(stream);

            var manifest = TomlSerializer.Deserialize<PackageManifest>(reader.ReadToEnd());

            if (manifest is null)
                throw new InvalidDataException("TOML manifest does not contain a valid package manifest.");

            if (string.IsNullOrWhiteSpace(manifest.Id))
                manifest.Id = _pathParser.GetDirectoryName(manifestPath);

            return manifest;
        }
        catch (Exception exception) when (exception is InvalidDataException or FormatException)
        {
            throw new InvalidDataException($"Invalid TOML package manifest: [{manifestPath}].", exception);
        }
    }

    public IReadOnlyList<DefRaw> ReadDefinitions(
        DefReadContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        using var stream = _fileStreamer.OpenRead(context.ResourcePath);
        using var reader = new StreamReader(stream);

        try
        {
            var table = TomlSerializer.Deserialize<TomlTable>(reader.ReadToEnd());

            if (table is null)
                throw Error(context, "TOML definition file is empty.");

            return [ReadDefinition(table, context)];
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw Error(context, $"Invalid TOML definition file: [{context.ResourcePath}].", exception);
        }
    }

    private DefRaw ReadDefinition(TomlTable table, DefReadContext context)
    {
        var typeName = ReadRequiredString(table, "$type", context);

        if (!_typeRegistry.TryResolveByElementName(typeName, out var type))
        {
            throw Error(context, $"Unknown definition type [{typeName}].");
        }

        var id = ReadRequiredId(table, context);
        var parentId = ReadOptionalParentId(table, context);

        var fields = ReadFields(table, context);

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

    private static DefId ReadRequiredId(TomlTable table, DefReadContext context)
    {
        var value = ReadRequiredString(table, "Id", context);

        try
        {
            return DefId.Parse(value);
        }
        catch (Exception exception)when (exception is ArgumentException or FormatException)
        {
            throw Error(context, $"Invalid definition Id [{value}].", exception);
        }
    }

    private static DefId? ReadOptionalParentId(TomlTable table, DefReadContext context)
    {
        if (!TryGetValue(table, "Parent", out var rawValue))
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

    private static Dictionary<string, DefValue> ReadFields(TomlTable table, DefReadContext context)
    {
        var fields = new Dictionary<string, DefValue>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in table)
        {
            if (ReservedProperties.Contains(pair.Key))
                continue;

            AddField(fields, pair.Key, ReadValue(pair.Value, context), context);
        }

        return fields;
    }

    private static DefValue ReadValue(object? value, DefReadContext context)
    {
        if (value is null)
            return DefValue.Null();

        return value switch
        {
            TomlTable table => ReadObject(table, context),
            TomlArray array => ReadArray(array, context),
            string stringValue => DefValue.ScalarValue(stringValue),
            bool boolValue => DefValue.ScalarValue(boolValue),
            sbyte sbyteValue => DefValue.ScalarValue(sbyteValue),
            byte byteValue => DefValue.ScalarValue(byteValue),
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
            _ => throw Error(context, $"Unsupported TOML value type [{value.GetType().FullName}].")
        };
    }

    private static DefValue ReadObject(TomlTable table, DefReadContext context)
    {
        var fields = new Dictionary<string, DefValue>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in table) 
            AddField(fields, pair.Key, ReadValue(pair.Value, context), context);

        return DefValue.ObjectValue(fields);
    }

    private static DefValue ReadArray(TomlArray array, DefReadContext context)
    {
        var values = new List<DefValue>(array.Count);

        foreach (var item in array) 
            values.Add(ReadValue(item, context));

        return DefValue.ListValue(values);
    }

    private static string ReadRequiredString(TomlTable table, string name, DefReadContext context)
    {
        if (!TryGetValue(table, name, out var value))
            throw Error(context, $"Definition does not contain required [{name}].");

        if (value is not string stringValue)
            throw Error(context, $"Definition [{name}] must be a string.");

        if (string.IsNullOrWhiteSpace(stringValue))
            throw Error(context, $"Definition [{name}] cannot be empty.");

        return stringValue;
    }

    private static bool TryGetValue(TomlTable table, string name, out object? value)
    {
        foreach (var pair in table)
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