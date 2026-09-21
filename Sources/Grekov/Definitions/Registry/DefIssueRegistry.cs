using Grekov.Definitions.Models;

namespace Grekov.Definitions.Registry;

public sealed class DefIssueRegistry
{
	private readonly Dictionary<string, List<DefIssue>> _errors = new(StringComparer.OrdinalIgnoreCase);

	public void Add(DefIssue issue)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(issue.PackageId);

		if (!_errors.TryGetValue(issue.PackageId, out var errors))
		{
			errors = [];
			_errors.Add(issue.PackageId, errors);
		}

		errors.Add(issue);
	}

	public IReadOnlyList<DefIssue> GetIssues(string packageId)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(packageId);

		return _errors.TryGetValue(packageId, out var errors)
			? errors
			: [];
	}

	public bool HasIssues(string packageId)
	{
		return _errors.TryGetValue(packageId, out var errors) && errors.Count > 0;
	}

	public void UnloadPackage(string packageId)
	{
		_errors.Remove(packageId);
	}
}