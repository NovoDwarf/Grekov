using Grekov.Packaging.Entities;

namespace Grekov.Assemblies.Interfaces;

public interface IPackagePathResolver
{
	public string GetAssembliesRoot(PackageInstance package);
	public string GetPhysicalAssembliesRoot(string packageAssembliesRoot);
}
