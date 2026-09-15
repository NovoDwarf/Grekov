using System.Xml;
using System.Xml.Linq;
using Grekov.Core;
using Grekov.Definitions.Interfaces;
using Grekov.Definitions.Models;
using Grekov.Definitions.Registry;

namespace Grekov.Readers.Xml;

internal sealed class DefXmlReader : IDefFormatReader
{
    private static readonly IReadOnlySet<string> SupportedExtensions = new HashSet<string>([".xml"], StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> ReservedAttributes = [with(StringComparer.OrdinalIgnoreCase), "Id", "Parent", "Type"];

    private readonly DefTypeRegistry _typeRegistry;

    public DefXmlReader(DefTypeRegistry typeRegistry)
    {
        ArgumentNullException.ThrowIfNull(typeRegistry);

        _typeRegistry = typeRegistry;
    }

    public IReadOnlySet<string> Extensions => SupportedExtensions;
    
    public IReadOnlyList<DefRaw> ReadDefs(DefReadContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var document = XDocument.Load(context.ResourcePath, LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);

        if (document.Root is null)
            throw new InvalidDataException($"XML definition file is empty: [{context.ResourcePath}].");

        return [ReadDefinition(document.Root, context)];
    }

    private DefRaw ReadDefinition(XElement element, DefReadContext context)
    {
        var elementName = element.Name.LocalName;

        if (!_typeRegistry.TryResolveByElementName(elementName, out var typeName))
            throw Error(element, context, $"Unknown definition type [{elementName}].");

        var id = ReadRequiredId(element, context);
        var parentId = ReadOptionalParentId(element, context);

        var fields = ReadFields(element, context);

        return new DefRaw
        {
            Id = id,
            TypeName = typeName,
            ParentId = parentId,
            Fields = fields,
            PackageId = context.PackageId,
            ResourcePath = context.ResourcePath
        };
    }

    private static DefId ReadRequiredId(XElement element, DefReadContext context)
    {
        var value = GetAttributeValue(element, "Id");

        if (string.IsNullOrWhiteSpace(value))
            throw Error(element, context, "Definition does not contain required [Id] attribute.");

        try
        {
            return DefId.Parse(value);
        }
        catch (Exception exception)when (exception is ArgumentException or FormatException)
        {
            throw Error(element, context, $"Invalid definition Id [{value}].", exception);
        }
    }

    private static DefId? ReadOptionalParentId(XElement element, DefReadContext context)
    {
        var value = GetAttributeValue(element, "Parent");

        if (string.IsNullOrWhiteSpace(value))
            return null;

        try
        {
            return DefId.Parse(value);
        }
        catch (Exception exception)when (exception is ArgumentException or FormatException)
        {
            throw Error(element, context, $"Invalid parent Id [{value}].", exception);
        }
    }

    private static Dictionary<string, DefValue> ReadFields(XElement element, DefReadContext context)
    {
        var fields = new Dictionary<string, DefValue>(StringComparer.OrdinalIgnoreCase);

        ReadAttributes(element, fields, context);
        ReadChildElements(element, fields, context);

        return fields;
    }

    private static void ReadAttributes(XElement element, Dictionary<string, DefValue> fields, DefReadContext context)
    {
        foreach (var attribute in element.Attributes())
        {
            var name = attribute.Name.LocalName;

            if (ReservedAttributes.Contains(name))
                continue;

            AddField(fields, name, DefValue.ScalarValue(attribute.Value), element, context);
        }
    }

    private static void ReadChildElements(XElement element, Dictionary<string, DefValue> fields, DefReadContext context)
    {
        foreach (var group in element.Elements().GroupBy(child => child.Name.LocalName, StringComparer.OrdinalIgnoreCase))
        {
            var children = group.ToArray();
            var fieldName = group.Key;

            DefValue value;

            if (children.Length == 1)
            {
                value = ReadElementValue(children[0], context);
            }
            else
            {
                value = DefValue.ListValue([.. children.Select(child => ReadElementValue(child, context))]);
            }

            AddField(fields, fieldName, value, element, context);
        }
    }

    private static DefValue ReadElementValue(XElement element, DefReadContext context)
    {
        var childElements = element.Elements().ToArray();
        var attributes = element.Attributes().ToArray();
        var text = element.Value.Trim();

        if (childElements.Length == 0 && attributes.Length == 0)
            return DefValue.ScalarValue(text);

        var fields = new Dictionary<string, DefValue>(StringComparer.OrdinalIgnoreCase);

        foreach (var attribute in attributes) 
            fields[attribute.Name.LocalName] = DefValue.ScalarValue(attribute.Value);

        foreach (var group in childElements.GroupBy(child => child.Name.LocalName, StringComparer.OrdinalIgnoreCase))
        {
            var children = group.ToArray();

            if (children.Length == 1)
                fields[group.Key] = ReadElementValue(children[0], context);
            else
                fields[group.Key] = DefValue.ListValue([.. children.Select(child => ReadElementValue(child, context))]);
        }

        return DefValue.ObjectValue(fields);
    }

    private static void AddField(Dictionary<string, DefValue> fields, string name, DefValue value, XElement element, DefReadContext context)
    {
        if (!fields.TryAdd(name, value))
            throw Error(element, context, $"Field [{name}] is declared more than once.");
    }

    private static string? GetAttributeValue(XElement element, string name)
    {
        return element.Attributes()
            .FirstOrDefault(attribute => attribute.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?.Value;
    }

    private static InvalidDataException Error(XElement element, DefReadContext context, string message, Exception? innerException = null)
    {
        IXmlLineInfo lineInfo = element;

        var location = lineInfo.HasLineInfo() ? $"Line {lineInfo.LineNumber}, position {lineInfo.LinePosition}. " : string.Empty;

        return new InvalidDataException($"{message} {location}. File: [{context.ResourcePath}']", innerException);
    }
}