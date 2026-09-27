using Grekov.Assemblies.Entities;
using Grekov.Defaults;
using Grekov.Packaging;
using Grekov.Packaging.Entities;
using Grekov.Packaging.Enums;
using Grekov.Packaging.Interfaces;
using NovoDwarf.FS.Paths.Interfaces;

namespace Grekov.Assemblies.Services;

internal sealed class AssemblyService
{
	private readonly AssemblyRegistry _registry;
	private readonly IPathParser _pathParser;

	public AssemblyService(AssemblyRegistry registry, IPathParser pathParser)
	{
		_registry = registry;
		_pathParser = pathParser;
	}

	public async Task Load(IReadOnlyList<PackageInstance> packages, CancellationToken token = default)
	{
		foreach (var package in packages)
		foreach (var path in Locate(package))
		{
			LoadAssembly(package.Id, path);
		}
	}

	public async Task Unload(IReadOnlyList<PackageInstance> packages, CancellationToken token = default)
	{
		foreach (var package in packages.Reverse())
		{
			_registry.RemovePackage(package.Id);
		}
	}

	public void Clear()
	{
		_registry.Clear();
	}

	public IReadOnlyList<string> Locate(PackageInstance package)
	{
		ArgumentNullException.ThrowIfNull(package);

		return package.Storage.EnumerateFiles(PackageContentType.Assembly);
	}
	
	private void LoadAssembly(string packageId, string path)
	{
		var filename = _pathParser.GetFileNameWithoutExtension(path);
		var loadContext = new EntrypointLoadContext(path, packageId, filename);

		try
		{
			var assembly = loadContext.LoadFromAssemblyPath(path);

			_registry.Add(packageId, assembly, loadContext);
		}
		catch
		{
			loadContext.Unload();
			throw;
		}
	}
}
