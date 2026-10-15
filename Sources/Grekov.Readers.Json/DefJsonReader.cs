using System.Globalization;
using System.Runtime.CompilerServices;
using Grekov.Abstractions;
using Grekov.Definitions.Registry;
using Grekov.Readers.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NovoDwarf.FS.Files.Interfaces;
using NovoDwarf.FS.Paths.Interfaces;


namespace Grekov.Readers.Json;

internal sealed class DefJsonReader : DefStructuredReader<JObject>
{
	private static readonly IReadOnlySet<string> SupportedExtensions =
		new HashSet<string>(
			[".json"],
			StringComparer.OrdinalIgnoreCase);

	private static readonly JsonSerializer ManifestSerializer =
		JsonSerializer.CreateDefault();

	public DefJsonReader(
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
			using var jsonReader = new JsonTextReader(reader);

			var manifest = ManifestSerializer.Deserialize<PackageManifest>(
				               jsonReader)
			               ?? throw new InvalidDataException(
				               "JSON manifest does not contain a valid package manifest.");

			if (string.IsNullOrWhiteSpace(manifest.Id) &&
			    !string.Equals(
				    source,
				    "<stream>",
				    StringComparison.Ordinal))
				manifest.Id = PathParser.GetDirectoryName(source);

			return manifest;
		}
		catch (JsonException exception)
		{
			throw new InvalidDataException(
				$"Invalid JSON package manifest: [{source}].",
				exception);
		}
	}

	protected override JObject Deserialize(Stream stream)
	{
		using var reader = new StreamReader(stream);

		return JObject.Parse(reader.ReadToEnd());
	}

	protected override bool TryGetValue(
		JObject document,
		string name,
		out object? value)
	{
		var property = document.Properties()
		                       .FirstOrDefault(property =>
			                       property.Name.Equals(
				                       name,
				                       StringComparison.OrdinalIgnoreCase));

		if (property is null)
		{
			value = null;
			return false;
		}

		value = property.Value;
		return true;
	}

	protected override IEnumerable<KeyValuePair<string, object?>> GetProperties(
		JObject document)
	{
		return document.Properties()
		               .Select(property =>
			               new KeyValuePair<string, object?>(
				               property.Name,
				               property.Value));
	}

	protected override DefValue ReadValue(
		object? value,
		DefReadContext context)
	{
		if (value is not JToken token)
		{
			Issues.Add(
				DefIssues.InvalidType(
					context.PackageId,
					context.ResourcePath,
					value?.ToString() ?? string.Empty,
					nameof(JToken)));

			return DefValue.Null();
		}

		return token.Type switch
		{
			JTokenType.Null =>
				DefValue.Null(),

			JTokenType.Object =>
				ReadObject((JObject)token, context),

			JTokenType.Array =>
				ReadArray((JArray)token, context),

			_ =>
				DefValue.ScalarValue(
					ReadScalarValue(token))
		};
	}

	private DefValue ReadObject(
		JObject document,
		DefReadContext context)
	{
		var values = new Dictionary<string, DefValue>(
			StringComparer.OrdinalIgnoreCase);

		foreach (var property in document.Properties())
			values[property.Name] =
				ReadValue(property.Value, context);

		return DefValue.ObjectValue(values);
	}

	private DefValue ReadArray(
		JArray array,
		DefReadContext context)
	{
		var values = new DefValue[array.Count];

		for (var i = 0; i < array.Count; i++) values[i] = ReadValue(array[i], context);

		return DefValue.ListValue(values);
	}

	private static string ReadScalarValue(JToken token)
	{
		return token.Type switch
		{
			JTokenType.Boolean =>
				token.Value<bool>().ToString(),

			JTokenType.Integer =>
				token.Value<long>().ToString(),

			JTokenType.Float =>
				token.Value<double>()
				     .ToString(CultureInfo.InvariantCulture),

			JTokenType.Date =>
				token.Value<DateTime>()
				     .ToString("O", CultureInfo.InvariantCulture),

			JTokenType.Guid =>
				token.Value<Guid>().ToString(),

			JTokenType.Uri =>
				token.Value<Uri>().ToString(),

			_ =>
				token.ToString()
		};
	}
}