using System.Xml.Serialization;
using Grekov.Core.Enums;

namespace Grekov.Packaging.Entities;

public sealed record PackageDependency
{
	[XmlAttribute("PackageId")]
	public string PackageId { get; init; } = string.Empty;

	[XmlAttribute("Version")]
	public string? Version { get; init; }

	[XmlAttribute("Operator")]
	public PackageDependencyOperator Operator { get; init; } = PackageDependencyOperator.Any;

	[XmlAttribute("Required")]
	public bool Required { get; init; } = true;
}