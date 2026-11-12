using System.Runtime.Loader;
using Grekov;
using Grekov.Abstractions;
using Grekov.Abstractions.Interfaces.Assemblies;
using Grekov.Assemblies.Services;
using Grekov.Defaults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Grekov.Tests;

public sealed class TestEntrypoint(EntrypointState state) : IEntrypoint, IAsyncDisposable
{
    public void OnLoad(IPackageContext context)
    {
        if (state.FailLoad)
        {
            throw new InvalidOperationException("Lifecycle failure");
        }
    }

    public void OnLoaded() { }

    public void OnUnload()
    {
        if (state.FailUnload)
        {
            throw new InvalidOperationException("Lifecycle failure");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await Task.Yield();
        state.Disposed = true;

        if (state.FailDispose)
        {
            throw new IOException("Disposal failure");
        }
    }
}
