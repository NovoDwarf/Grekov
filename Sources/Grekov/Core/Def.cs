using Grekov.Core.Attributes;

namespace Grekov.Core;

public abstract class Def
{
	public DefId Id { get; set; }
	
	[DefField]
	public string Name { get; set; } = string.Empty;
	
	public string Info => Name + "_info";
	public string Description => Name + "_description";
	public string Icon => Name + "_icon";
	
	public string PackageId { get; internal set; } = string.Empty;
	public string ResourcePath { get; internal set; } = string.Empty;
	public string ResourceName => Path.GetFileNameWithoutExtension(ResourcePath);
}
