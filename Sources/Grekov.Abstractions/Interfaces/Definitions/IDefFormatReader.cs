namespace Grekov.Abstractions.Interfaces.Definitions;

public interface IDefFormatReader
{
	public IReadOnlySet<string> Extensions { get; }

	PackageManifest ReadManifest(string manifestPath);

	PackageManifest ReadManifest(Stream stream);

	IReadOnlyList<DefRaw> ReadDefinitions(DefReadContext context);

	IReadOnlyList<DefRaw> ReadDefinitions(Stream stream, DefReadContext context);
}
