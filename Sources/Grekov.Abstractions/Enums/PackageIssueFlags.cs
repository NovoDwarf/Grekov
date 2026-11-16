namespace Grekov.Abstractions.Enums;

[Flags]
public enum PackageIssueFlags
{
	None = 0,
	Static = 1,
	BlocksLoading = 2
}
