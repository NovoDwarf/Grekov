using Grekov.Packaging.Entities;

namespace Grekov.Packaging.Interfaces;

public interface IPackageService
{
	public IReadOnlyList<PackageInstance> Packages { get; }
	public IReadOnlyList<PackageInstance> LoadOrder { get; }

	public Task Load(CancellationToken token = default);
	public Task Unload(CancellationToken token = default);
}
