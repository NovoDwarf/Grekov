using Grekov.Tests.Shared.Packaging;

namespace Grekov.Tests;

[Trait("Category", "Integration")]
public sealed class BinaryDiscoveryTests : BinaryDiscoveryTestBase
{
    [Fact]
    public async Task FailedDiscoveryClosesPreviouslyOpenedArchives()
    {
        // Arrange: the fixture provides a valid archive followed by a corrupt one.

        // Act
        var exception = await Record.ExceptionAsync(async () => await PackageProvider.DiscoverAsync());

        // Assert
        Assert.NotNull(exception);
        await using var exclusive = File.Open(ArchivePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
    }
}
