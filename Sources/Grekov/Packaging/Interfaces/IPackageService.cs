using Grekov.Packaging.Entities;

namespace Grekov.Packaging.Interfaces;

public interface IPackageService
{
	public IReadOnlyList<PackageInstance> Packages { get; }
	public IReadOnlyList<PackageInstance> LoadOrder { get; }

	public void LoadAll();
}
