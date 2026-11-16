using Grekov.Abstractions;

namespace Grekov.Tests.Shared.Packaging;

public static class PackageTestData
{
    public static PackageInstance Create(string id, string version = "1.0", params PackageDependency[] dependencies)
    {
        return new PackageInstance
        {
            Id = id,
            Version = version,
            Dependencies = dependencies,
            Container = new TestPackageContainer()
        };
    }
}
