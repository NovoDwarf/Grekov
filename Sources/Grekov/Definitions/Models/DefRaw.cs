using Grekov.Core;

namespace Grekov.Definitions.Models;

public sealed class DefRaw
{
	public required DefId Id { get; init; }

	public required string TypeName { get; init; }

	public DefId? ParentId { get; init; }

	public required IReadOnlyDictionary<string, DefValue> Fields { get; init; }

	public required string PackageId { get; init; }

	public required string ResourcePath { get; init; }
}