namespace Grekov.Tests.Shared.Assemblies;

public sealed class EntrypointState
{
    public bool FailLoad { get; init; }
    public bool FailUnload { get; init; }
    public bool FailDispose { get; init; }
    public bool Disposed { get; set; }
}
