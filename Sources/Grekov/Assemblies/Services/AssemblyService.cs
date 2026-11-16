using System.Runtime.Loader;
using Grekov.Abstractions;
using Grekov.Assemblies.Entities;

namespace Grekov.Assemblies.Services;

internal sealed class AssemblyService
{
    private readonly AssemblyRegistry _registry;

    public AssemblyService(AssemblyRegistry registry)
    {
        _registry = registry;
    }

    public Task LoadAsync(IReadOnlyList<PackageInstance> packages, CancellationToken token = default)
    {
        foreach (var package in packages)
        {
            token.ThrowIfCancellationRequested();
            var paths = Locate(package);

            if (paths.Count == 0)
            {
                continue;
            }

            var loadContext = new EntrypointLoadContext(package, paths);
            var registered = false;

            try
            {
                foreach (var path in paths)
                {
                    token.ThrowIfCancellationRequested();
                    var assembly = loadContext.LoadAssembly(path);

                    if (AssemblyLoadContext.GetLoadContext(assembly) == loadContext)
                    {
                        _registry.Add(package.Id, assembly, loadContext);
                        registered = true;
                    }
                }

                if (!registered)
                {
                    loadContext.Unload();
                }
            }
            catch
            {
                if (registered)
                {
                    _registry.RemovePackage(package.Id);
                }
                else
                {
                    loadContext.Unload();
                }

                throw;
            }
        }

        return Task.CompletedTask;
    }

    public Task UnloadAsync(IReadOnlyList<PackageInstance> packages, CancellationToken token = default)
    {
        foreach (var package in packages.Reverse())
        {
            token.ThrowIfCancellationRequested();
            _registry.RemovePackage(package.Id);
        }

        return Task.CompletedTask;
    }

    public void Clear()
    {
        _registry.Clear();
    }

    public IReadOnlyList<string> Locate(PackageInstance package)
    {
        ArgumentNullException.ThrowIfNull(package);

        return package.Container.EnumerateFiles(PackageContentType.Assembly)
            .Where(static path => string.Equals(Path.GetExtension(path), ".dll", StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }
}
