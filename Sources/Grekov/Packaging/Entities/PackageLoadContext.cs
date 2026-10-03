using Grekov.Abstractions;
using Grekov.Definitions.Registry;

namespace Grekov.Packaging.Entities;

public sealed record PackageLoadContext(PackageInstance Package, DefConflictRegistry Conflicts);
