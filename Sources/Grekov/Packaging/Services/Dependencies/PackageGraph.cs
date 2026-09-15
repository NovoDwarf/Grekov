using Grekov.Packaging.Entities;
using Grekov.Packaging.Services;

namespace Grekov.Packaging.Services.Dependencies;

internal static class PackageGraph
{
	public static IReadOnlyList<PackageInstance> TopologicalSort(
		IReadOnlyList<PackageInstance> packages,
		Func<string, PackageInstance?> resolvePackage,
		Action<PackageInstance, PackageIssue> addIssue)
	{
		var packageSet = packages.ToDictionary(static package => package.Id, StringComparer.OrdinalIgnoreCase);
		var ordered = new List<PackageInstance>(packages.Count);
		var states = new Dictionary<string, VisitState>(StringComparer.OrdinalIgnoreCase);

		foreach (var package in packages)
			Visit(package, packageSet, resolvePackage, addIssue, states, ordered);

		return [.. ordered.Where(static package => !package.HasErrors)];
	}

	private static void Visit(
		PackageInstance package,
		IReadOnlyDictionary<string, PackageInstance> packageSet,
		Func<string, PackageInstance?> resolvePackage,
		Action<PackageInstance, PackageIssue> addIssue,
		Dictionary<string, VisitState> states,
		List<PackageInstance> ordered)
	{
		if (states.TryGetValue(package.Id, out var state))
		{
			if (state == VisitState.Visiting)
				addIssue(package, PackageIssues.DependencyCycleIssue(package.Id));

			return;
		}

		states[package.Id] = VisitState.Visiting;

		foreach (var dependency in package.Dependencies.Where(static dependency => dependency.Required))
		{
			var dependencyPackage = packageSet.GetValueOrDefault(dependency.PackageId) ?? resolvePackage(dependency.PackageId);
			
			if (dependencyPackage == null)
			{
				addIssue(package, PackageIssues.MissingDependencyIssue(dependency.PackageId));
				continue;
			}

			if (packageSet.ContainsKey(dependencyPackage.Id))
				Visit(dependencyPackage, packageSet, resolvePackage, addIssue, states, ordered);
		}

		states[package.Id] = VisitState.Visited;

		if (!ordered.Contains(package))
			ordered.Add(package);
	}

	private enum VisitState
	{
		Visiting,
		Visited
	}
}
