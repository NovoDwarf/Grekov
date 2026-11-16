namespace Grekov.Abstractions.Interfaces.Definitions;

public interface IDefFormatReader
{
	public IReadOnlySet<string> Extensions { get; }

	public PackageManifest ReadManifest(string manifestPath);

	public PackageManifest ReadManifest(Stream stream);

	public IReadOnlyList<DefRaw> ReadDefinitions(DefReadContext context);

	public IReadOnlyList<DefRaw> ReadDefinitions(Stream stream, DefReadContext context);
}
