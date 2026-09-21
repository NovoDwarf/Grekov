using System.Reflection;
using System.Runtime.Loader;
using Grekov.Assemblies.Entities;
using Grekov.Extensions;
using Microsoft.Extensions.Options;

namespace Grekov.Assemblies.Services;

public sealed class AssemblyRegistry
{
    private readonly Dictionary<string, PackageAssemblies> _packages = new(StringComparer.OrdinalIgnoreCase);

    public AssemblyRegistry(IOptions<GrekovOptions> options)
    {
        AdditionalAssemblies =
        [
            .. options.Value.AdditionalAssemblies.Distinct()
        ];
    }

    public IReadOnlyList<Assembly> AdditionalAssemblies { get; }

    public IReadOnlyList<Assembly> GetAssemblies(string packageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);

        return _packages.TryGetValue(packageId, out var package)
            ? package.Assemblies
            : [];
    }

    public IReadOnlyList<Assembly> GetAllAssemblies()
    {
        return
        [
            .. _packages.Values
                .SelectMany(static package => package.Assemblies)
                .Concat(AdditionalAssemblies)
                .Distinct()
        ];
    }

    public void Add(
        string packageId,
        Assembly assembly,
        AssemblyLoadContext loadContext)
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

    public void AddRange(
        string packageId,
        IEnumerable<Assembly> assemblies,
        AssemblyLoadContext loadContext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);
        ArgumentNullException.ThrowIfNull(assemblies);
        ArgumentNullException.ThrowIfNull(loadContext);

        foreach (var assembly in assemblies)
            Add(packageId, assembly, loadContext);
    }

    public bool RemovePackage(string packageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);

        if (!_packages.Remove(packageId, out var package))
            return false;

        package.Unload();
        return true;
    }

    public void Clear()
    {
        foreach (var package in _packages.Values)
            package.Unload();

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
        private readonly HashSet<Assembly> _assemblies = [];
        private readonly HashSet<AssemblyLoadContext> _loadContexts = [];

        public PackageAssemblies(string packageId)
        {
            PackageId = packageId;
        }

        public string PackageId { get; }

        public IReadOnlyList<Assembly> Assemblies =>
            [.. _assemblies];

        public void Add(
            Assembly assembly,
            AssemblyLoadContext loadContext)
        {
            _assemblies.Add(assembly);
            _loadContexts.Add(loadContext);
        }

        public bool Contains(Assembly assembly)
        {
            return _assemblies.Contains(assembly);
        }

        public void Unload()
        {
            foreach (var loadContext in _loadContexts)
                loadContext.Unload();

            _assemblies.Clear();
            _loadContexts.Clear();
        }
    }
}