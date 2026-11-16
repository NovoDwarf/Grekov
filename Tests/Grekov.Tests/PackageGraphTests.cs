using Grekov.Tests.Shared.Packaging;
using Grekov.Abstractions;
using Grekov.Packaging.Services;

namespace Grekov.Tests;

[Trait("Category", "Unit")]
public sealed class PackageGraphTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RejectsUnavailableDependencyChains(bool disabled)
    {
        // Arrange
        var a = PackageTestData.Create("a", dependencies: [new PackageDependency { PackageId = "b" }]);
        var b = PackageTestData.Create("b", dependencies: [new PackageDependency { PackageId = "missing" }]);
        b.Enabled = !disabled;
        var packages = new[] { a, b };

        // Act
        var result = PackageGraph.TopologicalSort(packages.Where(p => p.Enabled).ToArray(),
            id => packages.SingleOrDefault(p => p.Id == id));

        // Assert
        Assert.Empty(result);
        Assert.True(a.HasErrors);
    }

    [Fact]
    public void RejectsEntireCycle()
    {
        // Arrange
        var a = PackageTestData.Create("a", dependencies: [new PackageDependency { PackageId = "b" }]);
        var b = PackageTestData.Create("b", dependencies: [new PackageDependency { PackageId = "a" }]);

        // Act
        var result = PackageGraph.TopologicalSort([a, b], _ => null);

        // Assert
        Assert.Empty(result);
        Assert.True(a.HasErrors);
        Assert.True(b.HasErrors);
    }

    [Theory]
    [ClassData(typeof(PackageVersionCases))]
    public void EnforcesVersions(string actual, string required, PackageDependencyOperator op, bool valid)
    {
        // Arrange
        var dependency = new PackageDependency { PackageId = "b", Version = required, Operator = op };
        var a = PackageTestData.Create("a", dependencies: [dependency]);
        var b = PackageTestData.Create("b", actual);

        // Act
        var result = PackageGraph.TopologicalSort([a, b], _ => null);

        // Assert
        Assert.Equal(valid, result.Contains(a));
    }
}
