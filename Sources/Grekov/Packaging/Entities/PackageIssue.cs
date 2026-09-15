using Grekov.Core.Enums;
using Grekov.Packaging.Enums;

namespace Grekov.Packaging.Entities;

public sealed record PackageIssue(
	PackageIssueSeverity Severity,
	string Code,
	string Message,
	PackageIssueFlags Flags = PackageIssueFlags.None)
{
	public bool BlocksLoading => Flags.HasFlag(PackageIssueFlags.BlocksLoading) ||
	                             Severity == PackageIssueSeverity.Error;
}
