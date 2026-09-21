namespace Grekov.Binary;

public sealed class GpakPackage
{
	public required GpakFile Manifest { get; init; }
	
	public IReadOnlyList<GpakFile> Assemblies { get; init; } = [];

	public IReadOnlyList<GpakFile> Definitions { get; init; } = [];

	public IReadOnlyList<GpakFile> Localization { get; init; } = [];

	public IReadOnlyList<GpakFile> Resources { get; init; } = [];
}