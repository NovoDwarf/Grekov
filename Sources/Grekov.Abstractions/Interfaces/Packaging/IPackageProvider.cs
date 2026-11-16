namespace Grekov.Abstractions.Interfaces.Packaging;

public interface IPackageProvider
{
	public ValueTask<IReadOnlyList<PackageInstance>> DiscoverAsync(CancellationToken cancellationToken = default);
}