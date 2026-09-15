namespace Grekov.Packaging.Entities;

public sealed class PackageDiscoveryResult
{
	public PackageDiscoveryResult(IReadOnlyList<PackageInstance> packages)
	{
		Packages = packages;
		PackagesById = packages.ToDictionary(static package => package.Id, StringComparer.OrdinalIgnoreCase);
	}

	public IReadOnlyList<PackageInstance> Packages { get; }
	public IReadOnlyDictionary<string, PackageInstance> PackagesById { get; }
}
