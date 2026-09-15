using System.Reflection;
using System.Runtime.Loader;

namespace Grekov.Assemblies.Entities;

internal sealed class EntrypointLoadContext : AssemblyLoadContext
{
	private readonly AssemblyDependencyResolver _resolver;

	public EntrypointLoadContext(string mainAssemblyPath, string packageId)
		: base(name: $"Package:{packageId}:{Path.GetFileNameWithoutExtension(mainAssemblyPath)}", isCollectible: true)
	{
		_resolver = new AssemblyDependencyResolver(mainAssemblyPath);
	}

	protected override Assembly? Load(AssemblyName assemblyName)
	{
		var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);

		return assemblyPath is null
			? null
			: LoadFromAssemblyPath(assemblyPath);
	}

	protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
	{
		var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);

		return libraryPath is null
			? IntPtr.Zero
			: LoadUnmanagedDllFromPath(libraryPath);
	}
}