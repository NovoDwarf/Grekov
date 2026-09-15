using Grekov.Packaging.Interfaces;

namespace Grekov.Packaging.Entities;

public sealed record PackageLoadContext(PackageInstance Package, IPackageConflictSink Conflicts);
