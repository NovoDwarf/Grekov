using Grekov.Abstractions;
using Grekov.Abstractions.Interfaces.Definitions;
using Grekov.Definitions.Registry;
using NovoDwarf.FS.Files.Interfaces;
using NovoDwarf.FS.Paths.Interfaces;

namespace Grekov.Readers.Abstractions;

public abstract class DefStructuredReader<TDocument> : IDefFormatReader
{
    protected static readonly IReadOnlySet<string> ReservedProperties =
        new HashSet<string>(
            ["$type", "Id", "Parent"],
            StringComparer.OrdinalIgnoreCase);

    protected readonly DefTypeRegistry TypeRegistry;
    protected readonly IFileStreamer FileStreamer;
    protected readonly IPathParser PathParser;
    protected readonly DefIssueRegistry Issues;

    protected DefStructuredReader(
        DefTypeRegistry typeRegistry,
        IPathParser pathParser,
        IFileStreamer fileStreamer,
        DefIssueRegistry issues)
    {
        TypeRegistry = typeRegistry;
        PathParser = pathParser;
        FileStreamer = fileStreamer;
        Issues = issues;
    }

    public abstract IReadOnlySet<string> Extensions { get; }

    public abstract PackageManifest ReadManifest(string manifestPath);

    public abstract PackageManifest ReadManifest(Stream stream);

    public IReadOnlyList<DefRaw> ReadDefinitions(DefReadContext context)
    {
        using var stream = FileStreamer.OpenRead(context.ResourcePath);

        return ReadDefinitions(stream, context);
    }

    public IReadOnlyList<DefRaw> ReadDefinitions(
        Stream stream,
        DefReadContext context)
    {
        var document = Deserialize(stream);

        if (document is null)
        {
            Issues.Add(
                DefIssues.FileEmpty(
                    context.PackageId,
                    context.ResourcePath));

            return [];
        }

        var definition = ReadDefinition(document, context);

        return definition is null
            ? []
            : [definition];
    }

    protected abstract TDocument Deserialize(Stream stream);

    protected abstract bool TryGetValue(
        TDocument document,
        string name,
        out object? value);

    protected abstract IEnumerable<KeyValuePair<string, object?>> GetProperties(
        TDocument document);

    protected abstract DefValue ReadValue(
        object? value,
        DefReadContext context);

    private DefRaw? ReadDefinition(
        TDocument document,
        DefReadContext context)
    {
        var typeName = ReadRequiredType(document, context);

        if (typeName is null)
            return null;

        if (!TypeRegistry.TryResolveByElementName(typeName, out var type))
        {
            Issues.Add(
                DefIssues.UnknownType(
                    context.PackageId,
                    context.ResourcePath,
                    typeName));

            return null;
        }

        var id = ReadRequiredId(document, context);

        if (id is null)
            return null;

        var parentId = ReadOptionalParentId(document, context);
        var fields = ReadFields(document, context);

        return new DefRaw
        {
            Id = id.Value,
            Type = type,
            ParentId = parentId,
            Fields = fields,
            PackageId = context.PackageId,
            ResourcePath = context.ResourcePath
        };
    }

    private string? ReadRequiredType(
        TDocument document,
        DefReadContext context)
    {
        if (!TryGetValue(document, "$type", out var rawValue))
        {
            Issues.Add(
                DefIssues.MissingProperty(
                    context.PackageId,
                    context.ResourcePath,
                    "$type"));

            return null;
        }

        if (rawValue is not string typeName ||
            string.IsNullOrWhiteSpace(typeName))
        {
            Issues.Add(
                DefIssues.InvalidPropertyType(
                    context.PackageId,
                    context.ResourcePath,
                    "$type",
                    rawValue?.ToString() ?? string.Empty,
                    "non-empty string"));

            return null;
        }

        return typeName;
    }

    private DefId? ReadRequiredId(
        TDocument document,
        DefReadContext context)
    {
        if (!TryGetValue(document, "Id", out var rawValue))
        {
            Issues.Add(
                DefIssues.MissingId(
                    context.PackageId,
                    context.ResourcePath));

            return null;
        }

        if (rawValue is not string value ||
            string.IsNullOrWhiteSpace(value))
        {
            Issues.Add(
                DefIssues.InvalidId(
                    context.PackageId,
                    context.ResourcePath,
                    rawValue?.ToString() ?? string.Empty));

            return null;
        }

        try
        {
            return DefId.Parse(value);
        }
        catch (Exception exception)
            when (exception is ArgumentException or FormatException)
        {
            Issues.Add(
                DefIssues.InvalidId(
                    context.PackageId,
                    context.ResourcePath,
                    value,
                    exception: exception));

            return null;
        }
    }

    private DefId? ReadOptionalParentId(
        TDocument document,
        DefReadContext context)
    {
        if (!TryGetValue(document, "Parent", out var rawValue))
            return null;

        if (rawValue is not string value)
        {
            Issues.Add(
                DefIssues.InvalidParentId(
                    context.PackageId,
                    context.ResourcePath,
                    rawValue?.ToString() ?? string.Empty));

            return null;
        }

        if (string.IsNullOrWhiteSpace(value))
            return null;

        try
        {
            return DefId.Parse(value);
        }
        catch (Exception exception)
            when (exception is ArgumentException or FormatException)
        {
            Issues.Add(
                DefIssues.InvalidParentId(
                    context.PackageId,
                    context.ResourcePath,
                    value,
                    exception: exception));

            return null;
        }
    }

    private Dictionary<string, DefValue> ReadFields(
        TDocument document,
        DefReadContext context)
    {
        var fields = new Dictionary<string, DefValue>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var pair in GetProperties(document))
        {
            if (ReservedProperties.Contains(pair.Key))
                continue;

            AddField(
                fields,
                pair.Key,
                ReadValue(pair.Value, context),
                context);
        }

        return fields;
    }

    private void AddField(
        Dictionary<string, DefValue> fields,
        string name,
        DefValue value,
        DefReadContext context)
    {
        if (fields.TryAdd(name, value))
            return;

        Issues.Add(
            DefIssues.DuplicateField(
                context.PackageId,
                context.ResourcePath,
                name));
    }
}