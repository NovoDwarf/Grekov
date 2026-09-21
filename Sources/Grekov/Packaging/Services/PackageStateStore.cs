using Grekov.Packaging.Entities;

namespace Grekov.Packaging.Services;

internal sealed class PackageStateStore
{
	public List<PackageInstance> Packages { get; } = [];

	public Dictionary<string, PackageInstance> PackagesById { get; } = new(StringComparer.OrdinalIgnoreCase);

	public IReadOnlyList<PackageInstance> LoadOrder { get; set; } = [];

	public async Task<PackagePersistentState> Load(CancellationToken token)
	{
		return new PackagePersistentState();
	}

	public async Task Save(CancellationToken state)
	{
		// persistent storage
	}
}