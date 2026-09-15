namespace Grekov.Packaging.Entities;

public sealed record PackageSnapshot(IReadOnlyList<PackageInstance> Packages, IReadOnlyList<PackageInstance> LoadOrder);
