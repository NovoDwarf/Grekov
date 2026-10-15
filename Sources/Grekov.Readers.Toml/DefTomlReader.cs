using Grekov.Abstractions;
using Grekov.Definitions.Registry;
using Grekov.Readers.Abstractions;
using NovoDwarf.FS.Files.Interfaces;
using NovoDwarf.FS.Paths.Interfaces;
using Tomlyn;
using Tomlyn.Model;

namespace Grekov.Readers.Toml;

internal sealed class DefTomlReader : DefStructuredReader<TomlTable>
{
	private static readonly IReadOnlySet<string> SupportedExtensions =
		new HashSet<string>(
			[".toml"],
			StringComparer.OrdinalIgnoreCase);

	public DefTomlReader(
		DefTypeRegistry typeRegistry,
		IPathParser pathParser,
		IFileStreamer fileStreamer,
		DefIssueRegistry issues)
		: base(typeRegistry, pathParser, fileStreamer, issues)
	{
	}

	public override IReadOnlySet<string> Extensions =>
		SupportedExtensions;

	public override PackageManifest ReadManifest(string manifestPath)
	{
		using var stream = FileStreamer.OpenRead(manifestPath);

		return ReadManifest(stream);
	}

	public override PackageManifest ReadManifest(Stream stream)
	{
		using var reader = new StreamReader(stream);

		return TomlSerializer.Deserialize<PackageManifest>(
			reader.ReadToEnd());
	}

	protected override TomlTable Deserialize(Stream stream)
	{
		using var reader = new StreamReader(stream);

		return TomlSerializer.Deserialize<TomlTable>(
			reader.ReadToEnd());
	}

	protected override bool TryGetValue(
		TomlTable document,
		string name,
		out object? value)
	{
		foreach (var pair in document)
		{
			if (!string.Equals(
				    pair.Key,
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
		TomlTable document)
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
			TomlTable table => ReadObject(table, context),
			TomlArray array => ReadArray(array, context),
			_ => DefValue.ScalarValue(value)
		};
	}

	private DefValue ReadObject(
		TomlTable table,
		DefReadContext context)
	{
		var values = new Dictionary<string, DefValue>(
			StringComparer.OrdinalIgnoreCase);

		foreach (var pair in table) values[pair.Key] = ReadValue(pair.Value, context);

		return DefValue.ObjectValue(values);
	}

	private DefValue ReadArray(
		TomlArray array,
		DefReadContext context)
	{
		var values = new DefValue[array.Count];

		for (var i = 0; i < array.Count; i++) values[i] = ReadValue(array[i], context);

		return DefValue.ListValue(values);
	}
}