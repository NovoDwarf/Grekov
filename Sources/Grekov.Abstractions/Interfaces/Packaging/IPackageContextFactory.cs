using Grekov.Abstractions.Interfaces.Assemblies;

namespace Grekov.Abstractions.Interfaces.Packaging;

public interface IPackageContextFactory
{
	public IPackageContext Create(PackageInstance package);
}
