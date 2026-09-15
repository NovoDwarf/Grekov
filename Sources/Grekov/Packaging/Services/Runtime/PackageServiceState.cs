using Grekov.Packaging.Entities;

namespace Grekov.Packaging.Services.Runtime;

internal sealed class PackageServiceState
{
	public List<PackageInstance> Packages { get; } = [];
	
	public Dictionary<string, PackageInstance> PackagesById { get; } = new(StringComparer.OrdinalIgnoreCase);
	
	public Dictionary<string, bool> EnabledOverrides { get; } = new(StringComparer.OrdinalIgnoreCase);

	public IReadOnlyList<PackageInstance> LoadOrder { get; set; } = [];
}
