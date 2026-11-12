using Grekov.Abstractions;
using Grekov.Packaging.Services;

namespace Grekov.Tests;

public sealed class PackageGraphTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RejectsUnavailableDependencyChains(bool disabled)
    {
        var a = Package("a", "b");
        var b = Package("b", "missing");
        b.Enabled = !disabled;
        var packages = new[] { a, b };
        Assert.Empty(PackageGraph.TopologicalSort(packages.Where(p => p.Enabled).ToArray(),
            id => packages.SingleOrDefault(p => p.Id == id)));
        Assert.True(a.HasErrors);
    }

    [Fact]
    public void RejectsEntireCycle()
    {
        var a = Package("a", "b");
        var b = Package("b", "a");
        Assert.Empty(PackageGraph.TopologicalSort([a, b], _ => null));
        Assert.True(a.HasErrors);
        Assert.True(b.HasErrors);
    }

    [Theory]
    [InlineData("1.0", "2.0", PackageDependencyOperator.Exact, false)]
    [InlineData("2.0", "2.0.0", PackageDependencyOperator.Exact, true)]
    [InlineData("2.1", "2.0", PackageDependencyOperator.AtLeast, true)]
    [InlineData("2.0-rc.1", "2.0", PackageDependencyOperator.AtLeast, false)]
    [InlineData("2.0-rc.10", "2.0-rc.2", PackageDependencyOperator.AtLeast, true)]
    [InlineData("invalid", "2.0", PackageDependencyOperator.AtLeast, false)]
    public void EnforcesVersions(string actual, string required, PackageDependencyOperator op, bool valid)
    {
        var a = new PackageInstance { Id = "a", Version = "1.0", Container = new TestPackageContainer(),
            Dependencies = [new PackageDependency { PackageId = "b", Version = required, Operator = op }] };
        var b = new PackageInstance { Id = "b", Version = actual, Dependencies = [], Container = new TestPackageContainer() };
        var result = PackageGraph.TopologicalSort([a, b], _ => null);
        Assert.Equal(valid, result.Contains(a));
    }

    private static PackageInstance Package(string id, string dependency) => new()
    {
        Id = id, Version = "1.0", Dependencies = [new PackageDependency { PackageId = dependency }],
        Container = new TestPackageContainer()
    };
}
