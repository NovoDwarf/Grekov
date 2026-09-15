using System.Xml.Serialization;

namespace Grekov.Packaging.Entities;

[XmlRoot("PackageManifest")]
public sealed class PackageManifest
{
	[XmlElement("Id")]
	public string Id { get; set; } = string.Empty;

	[XmlElement("Version")]
	public string Version { get; set; } = "1.0.0";

	[XmlElement("Enabled")]
	public bool Enabled { get; set; } = true;

	[XmlArray("Dependencies")]
	[XmlArrayItem("Dependency")]
	public List<PackageDependency> Dependencies { get; set; } = [];
}
