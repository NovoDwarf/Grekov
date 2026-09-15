using Grekov.Core.Enums;

namespace Grekov.Packaging.Entities;

public sealed class PackageInstance
{
	public PackageInstance(string id, string version, string rootPath)
	{
		Id = id;
		Version = version;
		RootPath = rootPath;
	}

	public string Id { get; }
	public string Version { get; }
	public string RootPath { get; }
	
	public bool Enabled { get; set; } = true;
	
	public PackageState State { get; set; } = PackageState.Discovered;
	
	public List<PackageDependency> Dependencies { get; } = [];
	public List<PackageIssue> Issues { get; } = [];

	public bool HasErrors => Issues.Any(static issue => issue.BlocksLoading);
	public string DefinitionsPath => Path.Combine(RootPath, "Definitions");

	public void AddIssue(PackageIssue issue) => Issues.Add(issue);
}
