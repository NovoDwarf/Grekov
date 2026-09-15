namespace Grekov.Definitions.Models;

public sealed record DefPendingReference(
	Type ExpectedType,
	string Id,
	string ResourcePath,
	Action<object?> Apply);
