namespace Grekov.Abstractions;

public sealed class DefRaw
{
	public required DefId Id { get; init; }
	public required Type Type { get; init; }

	public DefId? ParentId { get; init; }

	public required IReadOnlyDictionary<string, DefValue> Fields { get; init; }

	public required string PackageId { get; init; }
	public required string ResourcePath { get; init; }
}