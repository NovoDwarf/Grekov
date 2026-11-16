using System.Xml.Serialization;

namespace Grekov.Abstractions;

public sealed record PackageDependency
{
	public string PackageId { get; init; } = string.Empty;

	public string? Version { get; init; }

	public PackageDependencyOperator Operator { get; init; } = PackageDependencyOperator.Any;

	public bool Required { get; init; } = true;
}