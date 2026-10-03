using Grekov.Packaging.Enums;
using Grekov.Packaging.Interfaces;

namespace Grekov.Packaging.Entities;

public sealed class PackageInstance
{
	public required string Id { get; init; }
	public required string Version { get; init; }
	public required IReadOnlyList<PackageDependency> Dependencies { get; init; }
	public required IPackageContainer Container { get; init; }
	
	public List<PackageIssue> Issues { get; } = [];

	public bool Enabled { get; set; } = true;
	public bool HasErrors => Issues.Any(static issue => issue.BlocksLoading);
	
	public void AddIssue(PackageIssue issue) => Issues.Add(issue);
	
	public void Dispose() => Container.Dispose();
}
