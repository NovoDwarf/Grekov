using Grekov.Assemblies.Interfaces;
using Grekov.Packaging.Entities;
using NovoDwarf.FS.Interfaces;

namespace Grekov.Defaults;

public sealed class DefaultPathResolver : IPackagePathResolver
{
	private const string AssembliesFolderName = "Assemblies";

	private readonly IPathService _pathService;

	public DefaultPathResolver(IPathService pathService)
	{
		_pathService = pathService;
	}

	public string GetAssembliesRoot(PackageInstance package)
	{
		return _pathService.Combine(package.RootPath, AssembliesFolderName);
	}

	public string GetPhysicalAssembliesRoot(string packageAssembliesRoot)
	{
		return packageAssembliesRoot;
	}
}
