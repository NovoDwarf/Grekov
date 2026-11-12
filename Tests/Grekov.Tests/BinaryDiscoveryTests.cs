using Grekov.Abstractions.Interfaces.Packaging;
using Grekov.Binary;
using Grekov.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Grekov.Tests;

public sealed class BinaryDiscoveryTests
{
    [Fact]
    public async Task FailedDiscoveryClosesPreviouslyOpenedArchives()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"grekov-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var goodRoot = Path.Combine(directory, "good");
        var badRoot = Path.Combine(directory, "bad");
        Directory.CreateDirectory(goodRoot);
        Directory.CreateDirectory(badRoot);
        var manifest = Path.Combine(directory, "manifest.json");
        var archivePath = Path.Combine(goodRoot, "valid.gpak");

        try
        {
            File.WriteAllText(manifest, """{"Id":"valid","Version":"1.0"}""");
            using (var output = File.Create(archivePath))
            {
                new GpakWriter().Write(output, new GpakPackage { Manifest = new GpakFile(manifest, "manifest.json") });
            }

            var services = new ServiceCollection();
            services.AddGrekov(builder =>
            {
                builder.Storage(storage => storage.InMemory());
                builder.Readers(readers => readers.Json());
                builder.Providers(providers => providers.Binary(options => options.Roots.AddRange([goodRoot, badRoot])));
            });
            using var provider = services.BuildServiceProvider();
            var packages = Assert.Single(provider.GetServices<IPackageProvider>());
            var discovered = await packages.DiscoverAsync();
            Assert.Single(discovered).Dispose();
            File.WriteAllText(Path.Combine(badRoot, "invalid.gpak"), "invalid");
            await Assert.ThrowsAnyAsync<Exception>(async () => await packages.DiscoverAsync());
            using var exclusive = File.Open(archivePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
