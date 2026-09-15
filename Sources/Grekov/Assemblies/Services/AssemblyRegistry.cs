using System.Reflection;
using System.Runtime.Loader;
using Grekov.Assemblies.Entities;

namespace Grekov.Assemblies.Services;

public sealed class AssemblyRegistry
{
    private readonly Dictionary<string, PackageAssemblies> _packages = new(StringComparer.OrdinalIgnoreCase);

    private readonly IReadOnlyList<Assembly> _additionalAssemblies;

    public AssemblyRegistry(IEnumerable<Assembly> additionalAssemblies)
    {
        ArgumentNullException.ThrowIfNull(additionalAssemblies);

        _additionalAssemblies = [.. additionalAssemblies.Distinct()];
    }

    public IReadOnlyCollection<string> PackageIds => _packages.Keys;

    public IReadOnlyList<Assembly> AdditionalAssemblies => _additionalAssemblies;

    public IReadOnlyList<Assembly> GetAssemblies(string packageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);

        return _packages.TryGetValue(packageId, out var package)
            ? package.Assemblies
            : [];
    }

    public IReadOnlyCollection<Assembly> GetAllAssemblies() 
        => [.. _packages.Values.SelectMany(static package => package.Assemblies) .Concat(_additionalAssemblies).Distinct()];

    public void Add(string packageId, Assembly assembly, AssemblyLoadContext loadContext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(loadContext);

        if (!_packages.TryGetValue(packageId, out var package))
        {
            package = new PackageAssemblies(packageId);
            _packages.Add(packageId, package);
        }

        package.Add(assembly, loadContext);
    }

    public void AddRange(string packageId, IEnumerable<Assembly> assemblies, AssemblyLoadContext loadContext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);
        ArgumentNullException.ThrowIfNull(assemblies);
        ArgumentNullException.ThrowIfNull(loadContext);

        foreach (var assembly in assemblies)
        {
            Add(packageId, assembly, loadContext);
        }
    }

    public bool RemovePackage(string packageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);

        return _packages.Remove(packageId);
    }

    public void Clear()
    {
        foreach (var package in _packages.Values)
        {
            package.Unload();
        }

        _packages.Clear();
    }

    public bool Contains(string packageId, Assembly assembly)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);
        ArgumentNullException.ThrowIfNull(assembly);

        return _packages.TryGetValue(packageId, out var package) &&
               package.Contains(assembly);
    }

    private sealed class PackageAssemblies
    {
        private readonly List<Assembly> _assemblies = [];
        private readonly List<AssemblyLoadContext> _loadContexts = [];

        public PackageAssemblies(string packageId)
        {
            PackageId = packageId;
        }

        public string PackageId { get; }

        public IReadOnlyList<Assembly> Assemblies => _assemblies;

        public void Add(Assembly assembly, AssemblyLoadContext loadContext)
        {
            if (_assemblies.Contains(assembly))
                return;

            _assemblies.Add(assembly);

            if (!_loadContexts.Contains(loadContext)) 
                _loadContexts.Add(loadContext);
        }

        public bool Contains(Assembly assembly) => _assemblies.Contains(assembly);

        public void Unload()
        {
            foreach (var loadContext in _loadContexts) 
                loadContext.Unload();

            _assemblies.Clear();
            _loadContexts.Clear();
        }
    }
}