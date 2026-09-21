namespace Grekov.Packaging.Entities;

internal sealed class PackagePersistentState
{
	public List<string> LoadOrder { get; set; } = [];
	public Dictionary<string, string> PackageVersions { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}