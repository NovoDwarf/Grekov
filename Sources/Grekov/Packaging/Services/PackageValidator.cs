using Grekov.Abstractions;

namespace Grekov.Packaging.Services;

internal sealed class PackageValidator
{
	public void Validate(IReadOnlyList<PackageInstance> packages)
	{
		ValidateDuplicateIds(packages);
	}
	
	private static void ValidateDuplicateIds(IReadOnlyList<PackageInstance> packages)
	{
		var groups = packages
		             .Where(static package => !string.IsNullOrWhiteSpace(package.Id))
		             .GroupBy(static package => package.Id, StringComparer.OrdinalIgnoreCase);

		foreach (var group in groups)
		{
			if (group.Count() <= 1)
				continue;

			foreach (var package in group)
			{
				package.AddIssue(PackageIssue.DuplicatePackageIssue(package.Id));
			}
		}
	}
}