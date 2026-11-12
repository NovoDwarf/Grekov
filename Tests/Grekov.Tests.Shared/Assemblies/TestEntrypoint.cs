using Grekov.Abstractions.Interfaces.Assemblies;
using Grekov.Abstractions.Interfaces.Packaging;

namespace Grekov.Tests.Shared.Assemblies;

public sealed class TestEntrypoint(EntrypointState state) : IEntrypoint, IAsyncDisposable
{
    public void OnLoad(IPackageContext context)
    {
        if (state.FailLoad)
        {
            throw new InvalidOperationException("Lifecycle failure");
        }
    }

    public void OnLoaded()
    {
    }

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
