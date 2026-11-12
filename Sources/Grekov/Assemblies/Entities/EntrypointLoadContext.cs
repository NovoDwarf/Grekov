using System.Reflection;
using System.Runtime.Loader;
using Grekov.Abstractions;
using Grekov.Abstractions.Interfaces.Packaging;

namespace Grekov.Assemblies.Entities;

internal sealed class EntrypointLoadContext : AssemblyLoadContext
{
    private readonly IPackageContainer _container;
    private readonly Dictionary<string, string> _assemblyPaths = new(StringComparer.OrdinalIgnoreCase);

    public EntrypointLoadContext(PackageInstance package, IReadOnlyList<string> assemblyPaths)
        : base(name: $"Package:{package.Id}", isCollectible: true)
    {
        _container = package.Container;

        foreach (var path in assemblyPaths)
        {
            var name = Path.GetFileNameWithoutExtension(path);

            if (!_assemblyPaths.TryAdd(name, path))
            {
                throw new InvalidOperationException(
                    $"Package [{package.Id}] contains multiple assemblies named [{name}].");
            }
        }
    }

    public Assembly LoadAssembly(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var shared = FindSharedAssembly(name);

        if (shared is not null)
            return shared;

        var loaded = Assemblies.FirstOrDefault(assembly => string.Equals(assembly.GetName().Name, name, StringComparison.OrdinalIgnoreCase));

        if (loaded is not null)
            return loaded;

        using var stream = _container.OpenRead(PackageContentType.Assembly, path);
        return LoadFromStream(stream);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (assemblyName.Name is not { } name)
            return null;

        return FindSharedAssembly(name) ?? (_assemblyPaths.TryGetValue(name, out var path) ? LoadAssembly(path) : null);
    }

    private static Assembly? FindSharedAssembly(string name)
    {
        if (string.Equals(name, typeof(Def).Assembly.GetName().Name, StringComparison.OrdinalIgnoreCase))
            return typeof(Def).Assembly;

        if (string.Equals(name, typeof(GrekovOptions).Assembly.GetName().Name, StringComparison.OrdinalIgnoreCase))
            return typeof(GrekovOptions).Assembly;

        return null;
    }
}
