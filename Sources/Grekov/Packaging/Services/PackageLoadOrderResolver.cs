using Grekov.Packaging.Entities;

namespace Grekov.Packaging.Services;

internal sealed class PackageLoadOrderResolver
{
    public IReadOnlyList<PackageInstance> Resolve(PackageDiscoveryResult discovery, PackagePersistentState? state)
    {
        ArgumentNullException.ThrowIfNull(discovery);

        if (state is not null && TryRestore(discovery, state, out var loadOrder))
            return loadOrder;

        return BuildLoadOrder(discovery);
    }

    private static bool TryRestore(PackageDiscoveryResult discovery, PackagePersistentState state, out IReadOnlyList<PackageInstance> loadOrder)
    {
        loadOrder = [];

        if (state.LoadOrder.Count == 0)
            return false;

        if (state.LoadOrder.Count != discovery.Packages.Count)
            return false;

        if (state.LoadOrder.Any(packageId => !IsValidPackage(discovery, state, packageId)))
            return false;

        loadOrder = [.. state.LoadOrder.Select(id => discovery.PackagesById[id])];

        return true;
    }

    private static bool IsValidPackage(PackageDiscoveryResult discovery, PackagePersistentState state, string packageId)
    {
        if (!discovery.PackagesById.TryGetValue(packageId, out var package))
            return false;

        if (!state.PackageVersions.TryGetValue(package.Id, out var version))
            return false;

        if (!string.Equals(version, package.Version, StringComparison.OrdinalIgnoreCase))
            return false;

        return package is { Enabled: true, HasErrors: false };
    }

    public IReadOnlyList<PackageInstance> BuildLoadOrder(PackageDiscoveryResult discovery)
    {
        var candidates = discovery.Packages
                                  .Where(static package => package is { Enabled: true, HasErrors: false })
                                  .ToList();

        return PackageGraph.TopologicalSort(candidates, id => discovery.PackagesById.GetValueOrDefault(id));
    }
}