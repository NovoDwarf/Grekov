using System.Text.Json;
using Grekov.Core;
using Grekov.Definitions.Interfaces;
using Grekov.Definitions.Models;
using Grekov.Definitions.Registry;
using Grekov.Packaging.Entities;
using NovoDwarf.FS.Files.Interfaces;
using NovoDwarf.FS.Paths.Interfaces;

namespace Grekov.Readers.Json;

internal sealed class DefJsonReader : IDefFormatReader
{
    private static readonly IReadOnlySet<string> SupportedExtensions = new HashSet<string>([".json"], StringComparer.OrdinalIgnoreCase);
    private static readonly IReadOnlySet<string> ReservedProperties = new HashSet<string>(["Id", "Parent"], StringComparer.OrdinalIgnoreCase);
    private static readonly JsonSerializerOptions ManifestSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly DefTypeRegistry _typeRegistry;
    private readonly IFileStreamer _fileStreamer;
    private readonly IPathParser _pathParser;

    public DefJsonReader(
        DefTypeRegistry typeRegistry,
        IPathParser pathParser,
        IFileStreamer fileStreamer)
    {
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

            var manifest = JsonSerializer.Deserialize<PackageManifest>(stream, ManifestSerializerOptions);

            if (manifest is null)
                throw new InvalidDataException("JSON manifest does not contain a valid package manifest.");

            if (string.IsNullOrWhiteSpace(manifest.Id))
                manifest.Id = _pathParser.GetDirectoryName(manifestPath);

            return manifest;
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"Invalid JSON package manifest: [{manifestPath}].", exception);
        }
    }

    public IReadOnlyList<DefRaw> ReadDefinitions(DefReadContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        using var stream = _fileStreamer.OpenRead(context.ResourcePath);

        try
        {
            using var document = JsonDocument.Parse(stream);

            return [ReadDefinition(document.RootElement, context)];
        }
        catch (JsonException exception)
        {
            throw Error(context, $"Invalid JSON definition file: [{context.ResourcePath}].", exception);
        }
    }

    private DefRaw ReadDefinition(JsonElement element, DefReadContext context)
    {
        if (element.ValueKind != JsonValueKind.Object)
            throw Error(context, "JSON definition root must be an object.");

        var elementName = GetDefinitionTypeName(element, context);

        if (!_typeRegistry.TryResolveByElementName(elementName, out var typeName))
            throw Error(context, $"Unknown definition type [{elementName}].");

        var fields = ReadFields(element, context);

        var id = ReadRequiredId(element, context);
        var parentId = ReadOptionalParentId(element, context);

        fields.Remove("Id");
        fields.Remove("Parent");

        return new DefRaw
        {
            Id = id,
            Type = typeName,
            ParentId = parentId,
            Fields = fields,
            PackageId = context.PackageId,
            ResourcePath = context.ResourcePath
        };
    }

    private static string GetDefinitionTypeName(JsonElement element, DefReadContext context)
    {
        if (!element.TryGetProperty("$type", out var type))
            throw Error(context, "JSON definition does not contain required [$type].");

        if (type.ValueKind != JsonValueKind.String)
            throw Error(context, "Definition [$type] must be a string.");

        var value = type.GetString();

        if (string.IsNullOrWhiteSpace(value))
            throw Error(context, "Definition [$type] cannot be empty.");

        return value;
    }

    private static DefId ReadRequiredId(JsonElement element, DefReadContext context)
    {
        var value = GetStructuralValue(element, "Id", context);

        if (string.IsNullOrWhiteSpace(value))
            throw Error(context, "Definition does not contain required [Id].");

        try
        {
            return DefId.Parse(value);
        }
        catch (Exception exception)when (exception is ArgumentException or FormatException)
        {
            throw Error(context, $"Invalid definition Id [{value}].", exception);
        }
    }

    private static DefId? ReadOptionalParentId(JsonElement element, DefReadContext context)
    {
        var value = GetStructuralValue(element, "Parent", context);

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

    private static Dictionary<string, DefValue> ReadFields(JsonElement element, DefReadContext context)
    {
        var fields = new Dictionary<string, DefValue>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in element.EnumerateObject())
        {
            if (property.NameEquals("$type") || ReservedProperties.Contains(property.Name))
                continue;

            AddField(fields, property.Name, ReadValue(property.Value, context), context);
        }

        return fields;
    }

    private static DefValue ReadValue(JsonElement element, DefReadContext context)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null => DefValue.Null(),
            JsonValueKind.String => DefValue.ScalarValue(element.GetString() ?? string.Empty),
            JsonValueKind.Number => ReadNumber(element, context),
            JsonValueKind.True or JsonValueKind.False => DefValue.ScalarValue(element.GetBoolean()),
            JsonValueKind.Object => ReadObject(element, context),
            JsonValueKind.Array => ReadArray(element, context),
            _ => throw Error(context, $"Unsupported JSON value kind [{element.ValueKind}].")
        };
    }

    private static DefValue ReadObject(JsonElement element, DefReadContext context)
    {
        var fields = new Dictionary<string, DefValue>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in element.EnumerateObject()) 
            AddField(fields, property.Name, ReadValue(property.Value, context), context);

        return DefValue.ObjectValue(fields);
    }

    private static DefValue ReadArray(JsonElement element, DefReadContext context)
    {
        var values = new List<DefValue>();

        foreach (var item in element.EnumerateArray())
            values.Add(ReadValue(item, context));

        return DefValue.ListValue(values);
    }

    private static DefValue ReadNumber(JsonElement element, DefReadContext context)
    {
        if (element.TryGetInt32(out var int32))
            return DefValue.ScalarValue(int32);

        if (element.TryGetInt64(out var int64))
            return DefValue.ScalarValue(int64);

        if (element.TryGetDecimal(out var decimalValue))
            return DefValue.ScalarValue(decimalValue);

        if (element.TryGetDouble(out var doubleValue))
            return DefValue.ScalarValue(doubleValue);

        throw Error(context, $"Invalid JSON number [{element.GetRawText()}].");
    }

    private static string? GetStructuralValue(JsonElement element, string name, DefReadContext context)
    {
        JsonProperty? found = null;

        foreach (var property in element.EnumerateObject())
        {
            if (!property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                continue;

            if (found is not null)
                throw Error(context, $"Definition contains multiple [{name}] properties.");

            found = property;
        }

        if (found is null)
            return null;

        var value = found.Value.Value;

        if (value.ValueKind != JsonValueKind.String)
            throw Error(context, $"Definition [{name}] must be a string.");

        return value.GetString();
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