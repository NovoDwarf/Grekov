using Grekov.Tests.Shared.Assemblies;
using Grekov.Tests.Shared.Packaging;
using System.Runtime.Loader;
using Grekov;
using Grekov.Abstractions;
using Grekov.Abstractions.Interfaces.Assemblies;
using Grekov.Assemblies.Services;
using Grekov.Defaults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Grekov.Tests;

public sealed class EntrypointTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PreservesLifecycleAndDisposalFailures(bool loading)
    {
        // Arrange
        var state = new EntrypointState { FailLoad = loading, FailUnload = !loading, FailDispose = true };
        using var services = new ServiceCollection().AddSingleton(state).BuildServiceProvider();
        var registry = new AssemblyRegistry(Options.Create(new GrekovOptions()));
        registry.Add("test", typeof(TestEntrypoint).Assembly, AssemblyLoadContext.Default);
        var service = new EntrypointService(registry, services, new DefaultContextFactory(services, registry));
        var packages = new[] { new PackageInstance
        {
            Id = "test", Version = "1.0", Dependencies = [], Container = new TestPackageContainer()
        } };

        if (!loading)
        {
            await service.LoadAsync(packages);
        }

        // Act
        var exception = await Record.ExceptionAsync(() => loading
            ? service.LoadAsync(packages) : service.UnloadAsync(packages));

        // Assert
        var failure = Assert.IsType<AggregateException>(exception);
        Assert.Contains(failure.InnerExceptions, error => error.Message == "Lifecycle failure");
        Assert.Contains(failure.InnerExceptions, error => error.Message == "Disposal failure");
        Assert.True(state.Disposed);
    }

    [Fact]
    public async Task AwaitsAsynchronousDisposal()
    {
        // Arrange
        var state = new EntrypointState();
        using var services = new ServiceCollection().AddSingleton(state).BuildServiceProvider();
        var registry = new AssemblyRegistry(Options.Create(new GrekovOptions()));
        registry.Add("test", typeof(TestEntrypoint).Assembly, AssemblyLoadContext.Default);
        var service = new EntrypointService(registry, services, new DefaultContextFactory(services, registry));
        var packages = new[] { new PackageInstance
        {
            Id = "test", Version = "1.0", Dependencies = [], Container = new TestPackageContainer()
        } };
        await service.LoadAsync(packages);

        // Act
        await service.UnloadAsync(packages);

        // Assert
        Assert.True(state.Disposed);
    }
}
