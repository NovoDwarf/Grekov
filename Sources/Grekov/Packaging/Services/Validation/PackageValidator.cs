using Grekov.Packaging.Entities;

namespace Grekov.Packaging.Services.Validation;

internal sealed class PackageValidator
{
	public void Validate(IReadOnlyList<PackageInstance> packages)
	{
		ArgumentNullException.ThrowIfNull(packages);

		foreach (var package in packages)
		{
			if (string.IsNullOrWhiteSpace(package.Id))
			{
				package.AddIssue(PackageIssues.MissingManifestIdIssue(package.RootPath));
			}
		}

		var groups = packages
		             .Where(static package => !string.IsNullOrWhiteSpace(package.Id))
		             .GroupBy(static package => package.Id, StringComparer.OrdinalIgnoreCase);

		foreach (var group in groups)
		{
			if (group.Count() <= 1)
				continue;

			foreach (var package in group)
			{
				package.AddIssue(PackageIssues.DuplicatePackageIssue(package.Id));
			}
		}
	}
}