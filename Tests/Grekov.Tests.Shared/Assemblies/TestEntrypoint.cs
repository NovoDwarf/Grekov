using Grekov.Abstractions.Interfaces.Assemblies;
using Grekov.Abstractions.Interfaces.Packaging;

namespace Grekov.Tests.Shared.Assemblies;

public sealed class TestEntrypoint : IEntrypoint, IAsyncDisposable
{
    private readonly EntrypointState _state;

    public TestEntrypoint(EntrypointState state)
    {
        _state = state;
    }

    public void OnLoad(IPackageContext context)
    {
        if (_state.FailLoad)
            throw new InvalidOperationException("Lifecycle failure");
    }

    public void OnLoaded() { }

    public void OnUnload()
    {
        if (_state.FailUnload)
            throw new InvalidOperationException("Lifecycle failure");
    }

    public async ValueTask DisposeAsync()
    {
        await Task.Yield();
       
        _state.Disposed = true;

        if (_state.FailDispose)
            throw new IOException("Disposal failure");
    }
}
