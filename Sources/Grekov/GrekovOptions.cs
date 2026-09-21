using System.Reflection;

namespace Grekov;

public sealed class GrekovOptions
{
	public string ManifestName { get; set; } = "Manifest";
	public List<Assembly> AdditionalAssemblies { get; } = [];
}