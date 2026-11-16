using Grekov.Tests.Shared.Assemblies;
using Grekov.Tests.Shared.Definitions;
using Grekov.Tests.Shared.Infrastructure;

namespace Grekov.Tests;

[Trait("Category", "Integration")]
public sealed class AssemblyUnloadTests(ConversionFixture conversions, PackageAssemblyFixture assemblies)
    : IClassFixture<ConversionFixture>, IClassFixture<PackageAssemblyFixture>
{
    [Fact]
    public void MaterializedTypesDoNotKeepPackageAlive()
    {
        // Arrange
        var converter = conversions.CreateConverter();

        // Act
        var reference = assemblies.MaterializeAndUnload(converter);
        GarbageCollection.CollectUntilReleased(reference);

        // Assert
        Assert.False(reference.IsAlive);
    }
}
