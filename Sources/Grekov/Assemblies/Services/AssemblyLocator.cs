using Grekov.Assemblies.Interfaces;
using Grekov.Packaging.Entities;
using NovoDwarf.FS.Interfaces;

namespace Grekov.Assemblies.Services;

internal sealed class AssemblyLocator
{
	private readonly IPackagePathResolver _paths;
	private readonly IFileService _fileService;

	public AssemblyLocator(IPackagePathResolver paths, IFileService fileService)
	{
		_paths = paths;
		_fileService = fileService;
	}

	public IReadOnlyList<string> Locate(PackageInstance package)
	{
		var root = _paths.GetPhysicalAssembliesRoot(_paths.GetAssembliesRoot(package));

		if (_fileService.DirectoryExists(root))
			return [.. _fileService.EnumerateFiles(root, "*.dll", SearchOption.TopDirectoryOnly)];
		
		return (string[])[];
	}
}
