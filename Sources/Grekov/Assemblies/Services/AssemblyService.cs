using Grekov.Assemblies.Entities;
using Grekov.Defaults;
using Grekov.Packaging;
using Grekov.Packaging.Entities;
using Grekov.Packaging.Interfaces;

namespace Grekov.Assemblies.Services;

internal sealed class AssemblyService
{
	private readonly AssemblyLocator _locator;
	private readonly AssemblyRegistry _registry;

	public AssemblyService(AssemblyLocator locator, AssemblyRegistry registry)
	{
		_locator = locator;
		_registry = registry;
	}

	public void Load(IReadOnlyList<PackageInstance> packages)
	{
		ArgumentNullException.ThrowIfNull(packages);

		foreach (var package in packages)
		{
			foreach (var path in _locator.Locate(package))
			{
				LoadAssembly(package.Id, path);
			}
		}
	}

	public void Unload(IReadOnlyList<PackageInstance> packages)
	{
		ArgumentNullException.ThrowIfNull(packages);

		foreach (var package in packages.Reverse())
		{
			_registry.RemovePackage(package.Id);
		}
	}

	public void Clear()
	{
		_registry.Clear();
	}

	private void LoadAssembly(string packageId, string path)
	{
		var loadContext = new EntrypointLoadContext(path, packageId);

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