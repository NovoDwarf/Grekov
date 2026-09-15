using Grekov.Core.Interfaces;
using Grekov.Packaging.Entities;

namespace Grekov.Assemblies.Interfaces;

public interface IPackageContextFactory
{
	public IPackageContext Create(PackageInstance package);
}
