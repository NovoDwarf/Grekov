using System.Globalization;
using Grekov.Abstractions;
using Grekov.Definitions.Registry;
using Grekov.Readers.Abstractions;
using NovoDwarf.FS.Files.Interfaces;
using NovoDwarf.FS.Paths.Interfaces;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Grekov.Readers.Yaml;

internal sealed class DefYamlReader
	: DefStructuredReader<Dictionary<string, object?>>
{
	private static readonly IReadOnlySet<string> SupportedExtensions =
		new HashSet<string>(
			[".yaml", ".yml"],
			StringComparer.OrdinalIgnoreCase);

	private readonly IDeserializer _deserializer;

	public DefYamlReader(
		DefTypeRegistry typeRegistry,
		IPathParser pathParser,
		IFileStreamer fileStreamer,
		DefIssueRegistry issues,
		IDeserializer deserializer)
		: base(typeRegistry, pathParser, fileStreamer, issues)
	{
		_deserializer = deserializer;
	}

	public override IReadOnlySet<string> Extensions =>
		SupportedExtensions;

	public override PackageManifest ReadManifest(string manifestPath)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(manifestPath);

		using var stream = FileStreamer.OpenRead(manifestPath);

		return ReadManifest(stream, manifestPath);
	}

	public override PackageManifest ReadManifest(Stream stream)
	{
		return ReadManifest(stream, "<stream>");
	}

	private PackageManifest ReadManifest(
		Stream stream,
		string source)
	{
		try
		{
			using var reader = new StreamReader(stream);

			var manifest = _deserializer.Deserialize<PackageManifest>(reader)
			               ?? throw new InvalidDataException(
				               "YAML manifest does not contain a valid package manifest.");

			if (string.IsNullOrWhiteSpace(manifest.Id) &&
			    !string.Equals(
				    source,
				    "<stream>",
				    StringComparison.Ordinal))
				manifest.Id = PathParser.GetDirectoryName(source);

			return manifest;
		}
		catch (YamlException exception)
		{
			throw new InvalidDataException(
				$"Invalid YAML package manifest: [{source}].",
				exception);
		}
	}

	protected override Dictionary<string, object?> Deserialize(Stream stream)
	{
		using var reader = new StreamReader(stream);

		return _deserializer.Deserialize<Dictionary<string, object?>>(reader);
	}

	protected override bool TryGetValue(
		Dictionary<string, object?> document,
		string name,
		out object? value)
	{
		foreach (var pair in document)
		{
			if (!pair.Key.Equals(
				    name,
				    StringComparison.OrdinalIgnoreCase))
				continue;

			value = pair.Value;
			return true;
		}

		value = null;
		return false;
	}

	protected override IEnumerable<KeyValuePair<string, object?>> GetProperties(
		Dictionary<string, object?> document)
	{
		return document;
	}

	protected override DefValue ReadValue(
		object? value,
		DefReadContext context)
	{
		return value switch
		{
			null => DefValue.Null(),

			Dictionary<string, object?> dictionary =>
				ReadObject(dictionary, context),

			IList<object?> list =>
				ReadArray(list, context),

			_ => DefValue.ScalarValue(
				ConvertScalar(value))
		};
	}

	private DefValue ReadObject(
		Dictionary<string, object?> document,
		DefReadContext context)
	{
		var values = new Dictionary<string, DefValue>(
			StringComparer.OrdinalIgnoreCase);

		foreach (var pair in document) values[pair.Key] = ReadValue(pair.Value, context);

		return DefValue.ObjectValue(values);
	}

	private DefValue ReadArray(
		IList<object?> array,
		DefReadContext context)
	{
		var values = new DefValue[array.Count];

		for (var i = 0; i < array.Count; i++) values[i] = ReadValue(array[i], context);

		return DefValue.ListValue(values);
	}

	private static string ConvertScalar(object value)
	{
		return value switch
		{
			bool boolean =>
				boolean.ToString(),

			IFormattable formattable =>
				formattable.ToString(
					null,
					CultureInfo.InvariantCulture),

			_ =>
				value.ToString() ?? string.Empty
		};
	}
}