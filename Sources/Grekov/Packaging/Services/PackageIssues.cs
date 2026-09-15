using Grekov.Core.Enums;
using Grekov.Packaging.Entities;
using Grekov.Packaging.Enums;

namespace Grekov.Packaging.Services;

internal static class PackageIssues
{
	public static PackageIssue MissingManifestIdIssue(string path)
	{
		return Error("manifest.id.missing", $"Package manifest '{path}' has no id.");
	}

	public static PackageIssue DuplicatePackageIssue(string packageId)
	{
		return Error("package.duplicate", $"Package id '{packageId}' is declared more than once.");
	}

	public static PackageIssue MissingDependencyIssue(string packageId)
	{
		return Error("dependency.missing", $"Required package dependency '{packageId}' was not discovered.");
	}

	public static PackageIssue DependencyCycleIssue(string packageId)
	{
		return Error("dependency.cycle", $"Package dependency cycle includes '{packageId}'.");
	}

	public static PackageIssue LoaderFailureIssue(string loaderName, string message)
	{
		return Error("loader.failure", $"Loader '{loaderName}' failed: {message}");
	}

	public static PackageIssue RollbackFailureIssue(string message)
	{
		return Error("rollback.failure", $"Package reload rollback failed: {message}");
	}

	public static PackageIssue ConflictIssue(string key, string packageId, string previousPackageId)
	{
		return Error("conflict", $"Package '{packageId}' conflicts with package '{previousPackageId}' on key '{key}'.");
	}

	private static PackageIssue Error(string code, string message)
	{
		return new PackageIssue(PackageIssueSeverity.Error, code, message, PackageIssueFlags.Static | PackageIssueFlags.BlocksLoading);
	}
}
