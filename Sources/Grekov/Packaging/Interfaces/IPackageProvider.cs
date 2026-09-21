using Grekov.Packaging.Entities;

namespace Grekov.Packaging.Interfaces;

public interface IPackageProvider
{
	public ValueTask<IReadOnlyList<PackageInstance>> DiscoverAsync(CancellationToken cancellationToken = default);
}