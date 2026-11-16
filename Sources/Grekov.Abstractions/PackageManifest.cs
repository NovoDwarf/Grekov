namespace Grekov.Abstractions;

public sealed class PackageManifest
{
	public string Id { get; set; } = string.Empty;

	public string Version { get; set; } = "1.0.0";

	public bool Enabled { get; set; } = true;
	
	public List<PackageDependency> Dependencies { get; set; } = [];
}
