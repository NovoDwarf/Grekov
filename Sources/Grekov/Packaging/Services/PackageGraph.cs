using Grekov.Abstractions;

namespace Grekov.Packaging.Services;

internal static class PackageGraph
{
    public static IReadOnlyList<PackageInstance> TopologicalSort(IReadOnlyList<PackageInstance> packages, Func<string, PackageInstance?> resolvePackage)
    {
        ArgumentNullException.ThrowIfNull(packages);
        ArgumentNullException.ThrowIfNull(resolvePackage);

        var graph = new Graph(packages, resolvePackage);

        return graph.Sort();
    }

    private sealed class Graph
    {
        private readonly IReadOnlyDictionary<string, PackageInstance> _packages;
        private readonly Func<string, PackageInstance?> _resolvePackage;

        private readonly Dictionary<string, VisitState> _states = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<PackageInstance> _ordered = [];

        public Graph(IReadOnlyList<PackageInstance> packages, Func<string, PackageInstance?> resolvePackage)
        {
            _packages = packages.ToDictionary(static package => package.Id, StringComparer.OrdinalIgnoreCase);
            _resolvePackage = resolvePackage;
        }

        public IReadOnlyList<PackageInstance> Sort()
        {
            foreach (var package in _packages.Values)
                Visit(package);

            return [.. _ordered.Where(static package => !package.HasErrors)];
        }

        private void Visit(PackageInstance package)
        {
            if (IsVisited(package))
                return;

            if (IsVisiting(package))
            {
                package.AddIssue(PackageIssue.DependencyCycleIssue(package.Id));
                return;
            }

            _states[package.Id] = VisitState.Visiting;

            VisitDependencies(package);

            _states[package.Id] = VisitState.Visited;

            if (!package.HasErrors)
                _ordered.Add(package);
        }

        private void VisitDependencies(PackageInstance package)
        {
            foreach (var dependency in package.Dependencies)
            {
                if (!dependency.Required)
                    continue;

                var dependencyPackage = ResolveDependency(dependency.PackageId);

                if (dependencyPackage is null)
                {
                    package.AddIssue(PackageIssue.MissingDependencyIssue(dependency.PackageId));
                    continue;
                }

                Visit(dependencyPackage);
            }
        }

        private PackageInstance? ResolveDependency(string packageId)
        {
            return _packages.GetValueOrDefault(packageId) ?? _resolvePackage(packageId);
        }

        private bool IsVisiting(PackageInstance package)
        {
            return _states.TryGetValue(package.Id, out var state) && state == VisitState.Visiting;
        }

        private bool IsVisited(PackageInstance package)
        {
            return _states.TryGetValue(package.Id, out var state) && state == VisitState.Visited;
        }

        private enum VisitState
        {
            Visiting,
            Visited
        }
    }
}