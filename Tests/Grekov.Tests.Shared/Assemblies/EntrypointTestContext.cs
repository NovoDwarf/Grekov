using System.Runtime.Loader;
using Grekov.Abstractions;
using Grekov.Assemblies.Services;
using Grekov.Defaults;
using Grekov.Tests.Shared.Packaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Grekov.Tests.Shared.Assemblies;

public sealed class EntrypointTestContext : IAsyncDisposable
{
    private readonly ServiceProvider _services;
    internal EntrypointService Service { get; }
    public IReadOnlyList<PackageInstance> Packages { get; } = [PackageTestData.Create("test")];
    public EntrypointState State { get; }

    public EntrypointTestContext(EntrypointState? state = null)
    {
        State = state ?? new EntrypointState();
        _services = new ServiceCollection().AddSingleton(State).BuildServiceProvider();
        var registry = new AssemblyRegistry(Options.Create(new GrekovOptions()));
        registry.Add("test", typeof(TestEntrypoint).Assembly, AssemblyLoadContext.Default);
        Service = new EntrypointService(registry, _services, new DefaultContextFactory(_services, registry));
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await Service.UnloadAsync(Packages);
        }
        finally
        {
            await _services.DisposeAsync();
        }
    }
}
