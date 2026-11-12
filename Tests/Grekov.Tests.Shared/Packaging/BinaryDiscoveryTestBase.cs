using Grekov.Abstractions.Interfaces.Packaging;
using Grekov.Binary;
using Grekov.Extensions;
using Grekov.Tests.Shared.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Grekov.Tests.Shared.Packaging;

public abstract class BinaryDiscoveryTestBase : IAsyncLifetime
{
    private readonly TemporaryDirectory _directory = new();
    private ServiceProvider? _services;
    protected IPackageProvider PackageProvider { get; private set; } = null!;
    protected string ArchivePath { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var goodRoot = Path.Combine(_directory.Path, "good");
        var badRoot = Path.Combine(_directory.Path, "bad");
        Directory.CreateDirectory(goodRoot);
        Directory.CreateDirectory(badRoot);
        var manifest = Path.Combine(_directory.Path, "manifest.json");
        ArchivePath = Path.Combine(goodRoot, "valid.gpak");
        await File.WriteAllTextAsync(manifest, """{"Id":"valid","Version":"1.0"}""");

        await using (var output = File.Create(ArchivePath))
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
        _services = services.BuildServiceProvider();
        PackageProvider = _services.GetServices<IPackageProvider>().Single();
        var discovered = await PackageProvider.DiscoverAsync();

        try
        {
            if (discovered.Count != 1)
            {
                throw new InvalidOperationException("Expected one valid package during test preparation.");
            }
        }
        finally
        {
            foreach (var package in discovered)
            {
                package.Dispose();
            }
        }

        await File.WriteAllTextAsync(Path.Combine(badRoot, "invalid.gpak"), "invalid");
    }

    public async Task DisposeAsync()
    {
        try
        {
            if (_services is not null)
            {
                await _services.DisposeAsync();
            }
        }
        finally
        {
            _directory.Dispose();
        }
    }
}
