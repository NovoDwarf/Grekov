using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Grekov.Abstractions;
using Grekov.Abstractions.Enums;
using Grekov.Abstractions.Interfaces.Definitions;
using Grekov.Definitions.Registry;
using Microsoft.Extensions.Logging;
using NovoDwarf.FS.Files.Interfaces;
using NovoDwarf.FS.Paths.Interfaces;

namespace Grekov.Readers.Xml;

public sealed class DefXmlReader : IDefFormatReader
{
	private static readonly IReadOnlySet<string> SupportedExtensions = new HashSet<string>([".xml"], StringComparer.OrdinalIgnoreCase);
	private static readonly IReadOnlySet<string> ReservedAttributes = new HashSet<string>(["Id", "Parent"], StringComparer.OrdinalIgnoreCase);
	
	private static readonly XmlSerializer ManifestSerializer = new(typeof(PackageManifest));

	private readonly IFileStreamer _fileStreamer;
	private readonly IPathParser _pathParser;

	private readonly DefIssueRegistry _issueRegistry;
	private readonly DefTypeRegistry _typeRegistry;
	
	private readonly ILogger<DefXmlReader> _logger;
	
	public DefXmlReader(
		DefTypeRegistry typeRegistry,
		IPathParser pathParser,
		IFileStreamer fileStreamer, 
		DefIssueRegistry issueRegistry, 
		ILogger<DefXmlReader> logger)
	{
		_typeRegistry = typeRegistry;
		_pathParser = pathParser;
		_fileStreamer = fileStreamer;
		_issueRegistry = issueRegistry;
		_logger = logger;
	}

	public IReadOnlySet<string> Extensions => SupportedExtensions;

	public PackageManifest ReadManifest(string manifestPath)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(manifestPath);

		using var stream = _fileStreamer.OpenRead(manifestPath);
		return ReadManifest(stream, manifestPath);
	}

	public PackageManifest ReadManifest(Stream stream)
	{
		return ReadManifest(stream, "<stream>");
	}

	public IReadOnlyList<DefRaw> ReadDefinitions(DefReadContext context)
	{
		using var stream = _fileStreamer.OpenRead(context.ResourcePath);
		return ReadDefinitions(stream, context);
	}

	public IReadOnlyList<DefRaw> ReadDefinitions(Stream stream, DefReadContext context)
	{
		var document = XDocument.Load(stream, LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);

		if (document.Root is null)
		{
			_issueRegistry.Add(DefIssues.XmlEmpty(context.PackageId, context.ResourcePath));
			return [];
		}

		var definition = ReadDefinition(document.Root, context);

		return definition is null
			? [] 
			: [definition];
	}

	private PackageManifest ReadManifest(Stream stream, string source)
	{
		try
		{
			var manifest = ManifestSerializer.Deserialize(stream) as PackageManifest ?? throw new InvalidDataException("XML manifest does not contain a valid package manifest.");

			if (string.IsNullOrWhiteSpace(manifest.Id) && !string.Equals(source, "<stream>", StringComparison.Ordinal)) 
				manifest.Id = _pathParser.GetDirectoryName(source);

			return manifest;
		}
		catch (InvalidOperationException exception)
		{
			throw new InvalidDataException($"Invalid XML package manifest: [{source}].", exception);
		}
	}

	private DefRaw? ReadDefinition(XElement element, DefReadContext context)
	{
		var elementName = element.Name.LocalName;

		if (!_typeRegistry.TryResolveByElementName(elementName, out var typeName))
		{
			_issueRegistry.Add( DefIssues.UnknownType(context.PackageId, context.ResourcePath, elementName, GetLine(element), GetColumn(element)));
			return null;
		}

		var fields = ReadFields(element, context);
		var id = ReadRequiredId(element, context);
		
		if (id is null)
			return null;
		
		var parentId = ReadOptionalParentId(element, context);

		fields.Remove("Id");
		fields.Remove("Parent");

		return new DefRaw
		{
			Id = id.Value,
			Type = typeName,
			ParentId = parentId,
			Fields = fields,
			PackageId = context.PackageId,
			ResourcePath = context.ResourcePath
		};
	}

	private DefId? ReadRequiredId(XElement element, DefReadContext context)
	{
		var value = GetStructuralValue(element, "Id", context);

		if (string.IsNullOrWhiteSpace(value))
		{
			_issueRegistry.Add(DefIssues.MissingId(context.PackageId, context.ResourcePath, GetLine(element), GetColumn(element)));
			return null;
		}

		try
		{
			return DefId.Parse(value);
		}
		catch (Exception exception)when (exception is ArgumentException or FormatException)
		{
			_issueRegistry.Add(DefIssues.InvalidId(context.PackageId, context.ResourcePath, value, GetLine(element), GetColumn(element), exception));
			return null;
		}
	}

	private DefId? ReadOptionalParentId(XElement element, DefReadContext context)
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
			_issueRegistry.Add(DefIssues.InvalidParentId(context.PackageId, context.ResourcePath, value, GetLine(element), GetColumn(element), exception));
			return null;
		}
	}

	private Dictionary<string, DefValue> ReadFields(XElement element, DefReadContext context)
	{
		var fields = new Dictionary<string, DefValue>(StringComparer.OrdinalIgnoreCase);

		if (_logger.IsEnabled(LogLevel.Debug))
			LogValue("Definition", DefValue.ObjectValue(fields));
		
		ReadAttributes(element, fields, context);
		ReadChildElements(element, fields, context, element.Name.LocalName);

		return fields;
	}

	private void ReadAttributes(XElement element, Dictionary<string, DefValue> fields, DefReadContext context)
	{
		foreach (var attribute in element.Attributes())
		{
			var name = attribute.Name.LocalName;

			if (ReservedAttributes.Contains(name))
				continue;

			AddField(fields, name, DefValue.ScalarValue(attribute.Value), element, context);
		}
	}

	private void ReadChildElements(XElement element, Dictionary<string, DefValue> fields, DefReadContext context, string path)
	{
		foreach (var group in element.Elements().GroupBy(static child => child.Name.LocalName, StringComparer.OrdinalIgnoreCase))
		{
			var children = group.ToArray();
			var childPath = $"{path}.{group.Key}";
			var value = children.Length == 1
				? ReadElementValue(children[0], context, childPath)
				: DefValue.ListValue([.. children.Select(child => ReadElementValue(child, context, childPath))]);

			if (_logger.IsEnabled(LogLevel.Trace)) 
				_logger.LogTrace("XML [{Element}] -> field [{Field}] -> {Kind} ({Count})", element.Name.LocalName, group.Key, value.Kind, value.Kind == DefValueKind.List ? value.List!.Count : 1);

			AddField(fields, group.Key, value, element, context);
		}
	}

	private DefValue ReadElementValue(XElement element, DefReadContext context, string path)
	{
		var childElements = element.Elements().ToArray();
		var attributes = element.Attributes().ToArray();
    
		if (childElements.Length == 0 && attributes.Length == 0)
			return DefValue.ScalarValue(element.Value.Trim());

		var fields = new Dictionary<string, DefValue>(StringComparer.OrdinalIgnoreCase);

		foreach (var attribute in attributes) 
			AddField(fields, attribute.Name.LocalName, DefValue.ScalarValue(attribute.Value), element, context);

		ReadChildElements(element, fields, context, path);

		return DefValue.ObjectValue(fields);
	}

	private void AddField(Dictionary<string, DefValue> fields, string name, DefValue value, XElement element, DefReadContext context)
	{
		if (fields.TryAdd(name, value))
			return;

		_issueRegistry.Add(DefIssues.DuplicateField(context.PackageId, context.ResourcePath, name, GetLine(element), GetColumn(element)));
	}

	private string? GetStructuralValue(XElement element, string name, DefReadContext context)
	{
		var attribute = element.Attributes().FirstOrDefault(attribute => attribute.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase));
		var children = element.Elements()
		                      .Where(child => child.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase))
		                      .ToArray();

		if (attribute is not null && children.Length > 0)
		{
			_issueRegistry.Add(DefIssues.StructuralValueConflict(context.PackageId, context.ResourcePath, name, GetLine(element), GetColumn(element)));
			return null;
		}

		if (children.Length > 1)
		{
			_issueRegistry.Add(DefIssues.MultipleStructuralValues(context.PackageId, context.ResourcePath, name, GetLine(element), GetColumn(element)));
			return null;
		}

		return attribute?.Value ?? children.FirstOrDefault()?.Value;
	}

	private static int? GetLine(XElement element)
	{
		IXmlLineInfo lineInfo = element;

		return lineInfo.HasLineInfo()
			? lineInfo.LineNumber
			: null;
	}

	private static int? GetColumn(XElement element)
	{
		IXmlLineInfo lineInfo = element;

		return lineInfo.HasLineInfo()
			? lineInfo.LinePosition
			: null;
	}
	
	private void LogValue(string name, DefValue value, int depth = 0)
	{
		var indent = new string(' ', depth * 2);

		switch (value.Kind)
		{
			case DefValueKind.Null: _logger.LogIndentNameNull(indent, name); break;
			case DefValueKind.Scalar: _logger.LogIndentNameScalarValue(indent, name, value.Scalar); break;
			case DefValueKind.Object: _logger.LogIndentNameObject(indent, name);
				foreach (var entry in value.Object!)
					LogValue(entry.Key, entry.Value, depth + 1);

				break;

			case DefValueKind.List: _logger.LogIndentNameListCount(indent, name, value.List!.Count);
				for (var i = 0; i < value.List.Count; i++)
					LogValue($"[{i}]", value.List[i], depth + 1);

				break;
		}
	}
}