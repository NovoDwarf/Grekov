namespace Grekov.Definitions.Models;

public sealed record DefReadContext(
	string PackageId,
	string ResourcePath,
	string FallbackId,
	List<DefPendingReference> PendingReferences);
