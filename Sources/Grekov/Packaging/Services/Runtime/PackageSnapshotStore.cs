using Grekov.Packaging.Entities;

namespace Grekov.Packaging.Services.Runtime;

internal sealed class PackageSnapshotStore
{
	public PackageSnapshot? Current { get; private set; }

	public void Capture(IReadOnlyList<PackageInstance> packages, IReadOnlyList<PackageInstance> loadOrder)
	{
		Current = new PackageSnapshot([.. packages], [.. loadOrder]);
	}
}
