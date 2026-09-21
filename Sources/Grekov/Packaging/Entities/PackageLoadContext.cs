using Grekov.Definitions.Registry;
using Grekov.Packaging.Interfaces;
using Grekov.Packaging.Services;

namespace Grekov.Packaging.Entities;

public sealed record PackageLoadContext(PackageInstance Package, DefConflictRegistry Conflicts);
